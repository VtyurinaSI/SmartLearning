using System.Xml.Linq;

namespace CompilerService.Services
{
    public class CsprojParser
    {
        private IReadOnlyList<XElement> GetAllFields(string path, LoadOptions options= LoadOptions.PreserveWhitespace)
        {
            var doc = XDocument.Load(path, options);
            return doc.Descendants().ToList();
        }
        public string GetAssemblyName(string csprojPath)
        {
            string csprojName = Path.GetFileNameWithoutExtension(csprojPath)
                ?? throw new FileLoadException(csprojPath);
            
            var assemblyName = GetAllFields(csprojPath)
                .Where(x => x.Name.LocalName == "AssemblyName")
                .Select(x => x.Value?.Trim())
                .LastOrDefault(v => !string.IsNullOrWhiteSpace(v));

            return assemblyName ?? csprojName;
        }

        public string[] GetDependencies(string path)
        {
            return GetAllFields(path)
                .Where(x => x.Name.LocalName == "ProjectReference")
                .Select(x => x.Attribute("Include")?.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim()).ToArray();
        }

        public string[] GetPackages(string path)
        {
            return GetAllFields(path)
                .Where(x => x.Name.LocalName == "PackageReference")
                .Select(x => x.Attribute("Include")?.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim()).ToArray();
        }
    }
}