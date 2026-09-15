using System.Reflection;
using System.Runtime.CompilerServices;
using HydrateOrDiedrate.encumbrance;
using Vintagestory.API.Common;
using Vintagestory.Common;

var type = typeof(EntityBehaviorLiquidEncumbrance);
var snapshot = type.GetMethod("GetInventorySlotsSnapshot", BindingFlags.NonPublic | BindingFlags.Instance)!;
var incomplete = type.GetField("_inventoryScanIncomplete", BindingFlags.NonPublic | BindingFlags.Instance)!;

foreach (bool returnNull in new[] { false, true })
{
    var behavior = RuntimeHelpers.GetUninitializedObject(type);
    var backpack = (TestBackpack)RuntimeHelpers.GetUninitializedObject(typeof(TestBackpack));
    backpack.ReturnNull = returnNull;
    var slots = (List<ItemSlot>)snapshot.Invoke(behavior, new object[] { backpack })!;
    Assert(slots.Count == 1, "Partial snapshot must stop at unavailable slot");
    Assert((bool)incomplete.GetValue(behavior)!, "Partial scan must be marked incomplete");
}

var completeBehavior = RuntimeHelpers.GetUninitializedObject(type);
var complete = new DummyInventory(null, 2);
Assert(((List<ItemSlot>)snapshot.Invoke(completeBehavior, new object[] { complete })!).Count == 2,
    "Valid inventory must scan completely");
Assert(!(bool)incomplete.GetValue(completeBehavior)!, "Complete scan must not be marked incomplete");

bool propagated = false;
try
{
    snapshot.Invoke(completeBehavior, new object[] { new BrokenInventory() });
}
catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException)
{
    propagated = true;
}
Assert(propagated, "Unrelated inventory errors must propagate");
Console.WriteLine("PASS: partial backpack scans, complete scans, unrelated exception propagation");

static void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

sealed class TestBackpack : InventoryPlayerBackpacks
{
    public bool ReturnNull;
    public TestBackpack() : base("backpack-test", null) { }
    public override int Count => 3;
    public override ItemSlot this[int index]
    {
        get => index == 1 ? (ReturnNull ? null : throw new ArgumentOutOfRangeException(nameof(index))) : new DummySlot();
        set => throw new NotSupportedException();
    }
}

sealed class BrokenInventory : DummyInventory
{
    public BrokenInventory() : base(null, 1) { }
    public override ItemSlot this[int index]
    {
        get => throw new ArgumentOutOfRangeException(nameof(index));
        set => throw new NotSupportedException();
    }
}
