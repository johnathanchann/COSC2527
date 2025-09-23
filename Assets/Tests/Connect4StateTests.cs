using NUnit.Framework;
using System.Collections.Generic;
using ConnectFour;

public class Connect4StateTests
{
    [Test]
    public void HashAndEquality_SameState_AreEqual()
    {
        var state1 = new Connect4State();
        var state2 = state1.Clone(); // identical board and turn

        Assert.IsTrue(state1.Equals(state2), "States should be equal");
        Assert.AreEqual(state1.GetHashCode(), state2.GetHashCode(), "HashCodes should match");

        var dict = new Dictionary<Connect4State, string>();
        dict[state1] = "TestValue";

        Assert.IsTrue(dict.ContainsKey(state2), "Dictionary should find key by equivalent state");
        Assert.AreEqual("TestValue", dict[state2]);
    }

    [Test]
    public void HashAndEquality_DifferentState_NotEqual()
    {
        var state1 = new Connect4State();
        var state2 = new Connect4State();

        // Force a move so they are not the same
        state2.MakeMove(0);

        Assert.IsFalse(state1.Equals(state2), "States should not be equal");
        Assert.AreNotEqual(state1.GetHashCode(), state2.GetHashCode(), "HashCodes should not match");

        var dict = new Dictionary<Connect4State, string>();
        dict[state1] = "State1";

        Assert.IsFalse(dict.ContainsKey(state2), "Dictionary should not find a different state");
    }
}
