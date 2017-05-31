using VDS.RDF;

namespace NetworkedPlanet.Quince
{
    public interface IPathProvider
    {
        string GetSubjectPath(INode node);
        string GetObjectPath(INode node);
    }
}
