namespace Santase.Logic
{
    // http://blogs.msdn.com/b/brada/archive/2004/05/03/125427.aspx
    public interface IDeepCloneable<out T>
    {
        T DeepCopy();
    }
}
