using System.Windows.Media.Media3D;

namespace Wpf3DApp
{
    public class ModelVisual3DWithName: ModelVisual3D
    {
        public string Name { get; set; }
        public object Tag { get; set; }
    }
}