namespace Santase.Logic
{
    public interface IDeepCloneable<out T>
    {
        T DeepClone();
    }
}
