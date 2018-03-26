namespace MobyInspector
{
    public interface IPatherator
    {
        (string searchPath, string fileWhiteout, string pathWhiteout) Parse(string search);
    }
}