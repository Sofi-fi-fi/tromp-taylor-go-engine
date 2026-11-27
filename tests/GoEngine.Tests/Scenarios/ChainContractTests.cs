using GoEngine.Core;
using GoEngine.Tests.Support;
using Xunit;

namespace GoEngine.Tests.Scenarios;

public abstract class ChainContractTests : EngineContractTests
{
    // Comment 3: only stones form strings, so an empty point has neither a chain nor liberties.
    [Fact]
    public void EmptyPoint_HasEmptyChainAndNoLiberties()
    {
        IGoEngine engine = Load(
            "...",
            ".X.",
            "...");

        Assert.Empty(engine.GetChain(new Point(0, 0)));
        Assert.Empty(engine.GetLiberties(new Point(0, 0)));
    }

    // Comment 3: the liberties of a lone stone are its empty orthogonal neighbours on the board.
    [Theory]
    [InlineData(2, 2, new[] { 2, 1, 1, 2, 3, 2, 2, 3 })]
    [InlineData(2, 0, new[] { 1, 0, 3, 0, 2, 1 })]
    [InlineData(0, 0, new[] { 1, 0, 0, 1 })]
    public void SingleStone_LibertiesAreOnBoardNeighbours(int x, int y, int[] liberties)
    {
        char[][] rows = [.. Enumerable.Range(0, 5).Select(_ => ".....".ToCharArray())];
        rows[y][x] = 'X';
        IGoEngine engine = Load([.. rows.Select(r => new string(r))]);

        AssertPoints([new Point(x, y)], engine.GetChain(new Point(x, y)));
        AssertPoints(Points(liberties), engine.GetLiberties(new Point(x, y)));
    }

    // Rule 3 and comment 3: stones of one colour connected orthogonally form one string.
    [Fact]
    public void OrthogonalNeighbours_FormOneChain()
    {
        IGoEngine engine = Load(
            ".....",
            ".XX..",
            "..X..",
            ".....",
            ".....");

        AssertPoints([new(1, 1), new(2, 1), new(2, 2)], engine.GetChain(new Point(1, 1)));
        AssertPoints(
            [new(1, 0), new(2, 0), new(0, 1), new(3, 1), new(1, 2), new(3, 2), new(2, 3)],
            engine.GetLiberties(new Point(1, 1)));
    }

    // Rule 3: adjacency is orthogonal only, so diagonal stones are separate strings.
    [Fact]
    public void DiagonalNeighbours_AreSeparateChains()
    {
        IGoEngine engine = Load(
            "....",
            ".X..",
            "..X.",
            "....");

        AssertPoints([new(1, 1)], engine.GetChain(new Point(1, 1)));
        AssertPoints([new(2, 2)], engine.GetChain(new Point(2, 2)));
        AssertPoints([new(1, 0), new(0, 1), new(2, 1), new(1, 2)], engine.GetLiberties(new Point(1, 1)));
    }

    // Comment 3: a string and its liberties do not depend on which of its stones is queried.
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public void AnyStoneOfChain_GivesSameChainAndLiberties(int x, int y)
    {
        IGoEngine engine = Load(
            "XX.",
            ".X.",
            "...");

        AssertPoints([new(0, 0), new(1, 0), new(1, 1)], engine.GetChain(new Point(x, y)));
        AssertPoints([new(2, 0), new(0, 1), new(2, 1), new(1, 2)], engine.GetLiberties(new Point(x, y)));
    }

    // Comment 3: liberties are a set of points; (2,0) touches both (1,0) and (2,1).
    [Fact]
    public void SharedLiberty_IsCountedOnce()
    {
        IGoEngine engine = Load(
            ".X..",
            ".XX.",
            "....",
            "....");

        IReadOnlySet<Point> liberties = engine.GetLiberties(new Point(1, 0));

        Assert.Equal(6, liberties.Count);
        AssertPoints([new(0, 0), new(2, 0), new(0, 1), new(3, 1), new(1, 2), new(2, 2)], liberties);
    }

    // Rule 3 and comment 3: a string reaches only its own colour; liberties are empty points only.
    [Fact]
    public void AdjacentOpponentStones_AreNeitherMembersNorLiberties()
    {
        IGoEngine engine = Load(
            ".O.",
            "OXO",
            "...");

        AssertPoints([new(1, 1)], engine.GetChain(new Point(1, 1)));
        AssertPoints([new(1, 2)], engine.GetLiberties(new Point(1, 1)));
        AssertPoints([new(1, 0)], engine.GetChain(new Point(1, 0)));
        AssertPoints([new(0, 0), new(2, 0)], engine.GetLiberties(new Point(1, 0)));
    }

    // Rule 3: a path through an empty point does not connect stones into one string.
    [Fact]
    public void SameColourChainsSeparatedByEmptyPoint_StaySeparate()
    {
        IGoEngine engine = Load(
            "X.X",
            "...",
            "...");

        AssertPoints([new(0, 0)], engine.GetChain(new Point(0, 0)));
        AssertPoints([new(2, 0)], engine.GetChain(new Point(2, 0)));
        AssertPoints([new(1, 0), new(0, 1)], engine.GetLiberties(new Point(0, 0)));
    }

    // Comment 3: an empty point inside the string is reached by it, so it is a liberty.
    [Fact]
    public void ChainAroundEye_EyeIsLiberty()
    {
        IGoEngine engine = Load(
            "XXX..",
            "X.X..",
            "XXX..",
            ".....",
            ".....");

        AssertPoints(
            [new(0, 0), new(1, 0), new(2, 0), new(0, 1), new(2, 1), new(0, 2), new(1, 2), new(2, 2)],
            engine.GetChain(new Point(1, 2)));
        AssertPoints(
            [new(3, 0), new(1, 1), new(3, 1), new(3, 2), new(0, 3), new(1, 3), new(2, 3)],
            engine.GetLiberties(new Point(1, 2)));
    }

    // Comment 3: strings are defined for both colours alike.
    [Fact]
    public void WhiteChain_BehavesLikeBlack()
    {
        IGoEngine engine = Load(
            ".....",
            ".OO..",
            ".XO..",
            ".....",
            ".....");

        AssertPoints([new(1, 1), new(2, 1), new(2, 2)], engine.GetChain(new Point(2, 2)));
        AssertPoints(
            [new(1, 0), new(2, 0), new(0, 1), new(3, 1), new(3, 2), new(2, 3)],
            engine.GetLiberties(new Point(2, 2)));
    }

    // Rule 3: reachability spans the whole board; one empty point is the only liberty.
    [Fact]
    public void LargestBoard_AllBlackButOnePoint_ChainCoversBoard()
    {
        string[] rows = [.. Enumerable.Repeat(new string('X', 19), 19)];
        rows[9] = new string('X', 9) + "." + new string('X', 9);
        IGoEngine engine = Load(rows);

        IReadOnlySet<Point> chain = engine.GetChain(new Point(0, 0));

        Assert.Equal(360, chain.Count);
        Assert.DoesNotContain(new Point(9, 9), chain);
        AssertPoints([new(9, 9)], engine.GetLiberties(new Point(18, 18)));
    }

    // Comment 3 on the smallest test board.
    [Fact]
    public void SmallestBoard_SingleStone()
    {
        IGoEngine engine = Load(
            "X.",
            "..");

        AssertPoints([new(0, 0)], engine.GetChain(new Point(0, 0)));
        AssertPoints([new(1, 0), new(0, 1)], engine.GetLiberties(new Point(0, 0)));
    }

    // Rule 1: points exist only on the grid.
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(3, 0)]
    [InlineData(0, 3)]
    public void GetChain_PointOutsideBoard_Throws(int x, int y)
    {
        IGoEngine engine = Load(
            "X..",
            "...",
            "...");

        Assert.Throws<ArgumentOutOfRangeException>(() => engine.GetChain(new Point(x, y)));
    }

    // Rule 1: points exist only on the grid.
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(3, 0)]
    [InlineData(0, 3)]
    public void GetLiberties_PointOutsideBoard_Throws(int x, int y)
    {
        IGoEngine engine = Load(
            "X..",
            "...",
            "...");

        Assert.Throws<ArgumentOutOfRangeException>(() => engine.GetLiberties(new Point(x, y)));
    }

    // Rule 2: a position is the colouring of every point; loading keeps it and the game state as given.
    [Fact]
    public void Snapshot_AfterLoading_EqualsLoadedPosition()
    {
        PositionSnapshot position = Diagram.Parse(
            [
                "XO.",
                ".X.",
                "..O",
            ],
            Color.White,
            consecutivePasses: 1,
            blackStonesRemoved: 3,
            whiteStonesRemoved: 2);
        IGoEngine engine = CreateEngine(RuleSet.CreateForTests(3), position);

        Assert.Equal(position, engine.Snapshot());
    }

    // Rule 1: the grid size is fixed by the rules, so a position of another size cannot be loaded.
    [Fact]
    public void Loading_SizeDifferentFromRules_Throws()
    {
        PositionSnapshot position = Diagram.Parse(
            "....",
            "....",
            "....",
            "....");

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => CreateEngine(RuleSet.CreateForTests(3), position));

        Assert.Equal("position", ex.ParamName);
    }

    // Comment 4: strings without liberties cannot exist on the board.
    [Fact]
    public void Loading_BlackChainWithoutLiberties_Throws()
    {
        PositionSnapshot position = Diagram.Parse(
            "XO.",
            "O..",
            "...");

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => CreateEngine(RuleSet.CreateForTests(3), position));

        Assert.Equal("position", ex.ParamName);
    }

    // Comment 4: strings without liberties cannot exist on the board.
    [Fact]
    public void Loading_WhiteChainWithoutLiberties_Throws()
    {
        PositionSnapshot position = Diagram.Parse(
            "OX.",
            "X..",
            "...");

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => CreateEngine(RuleSet.CreateForTests(3), position));

        Assert.Equal("position", ex.ParamName);
    }

    // Comment 4: strings without liberties cannot exist on the board. The dead White chain comes
    // after live Black stones in row-major order, so checking only the first stone is not enough.
    [Fact]
    public void Loading_DeadMultiStoneChainAfterLiveStones_Throws()
    {
        PositionSnapshot position = Diagram.Parse(
            "...",
            ".XX",
            "XOO");

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => CreateEngine(RuleSet.CreateForTests(3), position));

        Assert.Equal("position", ex.ParamName);
    }

    // Comment 4: on a full board no string has a liberty.
    [Fact]
    public void Loading_FullBoard_Throws()
    {
        PositionSnapshot position = Diagram.Parse(
            "XXO",
            "XOO",
            "XXO");

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => CreateEngine(RuleSet.CreateForTests(3), position));

        Assert.Equal("position", ex.ParamName);
    }

    // IGoEngine: returned sets are copies owned by the caller, so clearing a mutable result must not
    // affect the engine; a read-only set cannot be corrupted by the caller.
    [Fact]
    public void GetChain_ReturnsCopy()
    {
        IGoEngine engine = Load(
            "XX.",
            "...",
            "...");

        if (engine.GetChain(new Point(0, 0)) is ISet<Point> { IsReadOnly: false } first)
        {
            first.Clear();
        }

        AssertPoints([new(0, 0), new(1, 0)], engine.GetChain(new Point(0, 0)));
    }

    // IGoEngine: returned sets are copies owned by the caller; see GetChain_ReturnsCopy.
    [Fact]
    public void GetLiberties_ReturnsCopy()
    {
        IGoEngine engine = Load(
            "XX.",
            "...",
            "...");

        if (engine.GetLiberties(new Point(0, 0)) is ISet<Point> { IsReadOnly: false } first)
        {
            first.Clear();
        }

        AssertPoints([new(2, 0), new(0, 1), new(1, 1)], engine.GetLiberties(new Point(0, 0)));
    }
}
