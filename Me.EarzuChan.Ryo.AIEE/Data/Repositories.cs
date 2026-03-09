namespace Me.EarzuChan.Ryo.AIEE.Data;

public class NewMassRepository
{
    private int _newMassCounter;
    
    public int GetAndIncrementNewMassCounter() => Interlocked.Increment(ref _newMassCounter);
}