using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Globalization;
using System.IO;

namespace Wpf3DApp
{
    public class WavefrontObjLoader
    {

        private static readonly char[] CommentCharacters = new char[] { '#', '$' };
        private List<Point3D> coordinates;
        private static readonly Material DefaultWhiteMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.White));
        private List<Face> faces;
        private static readonly double GeneratedVertexTolerance = 1E-06;
        private bool generateNormals = true;
        private bool generateTextureCoordinates = true;
        private List<Group> groups;
        private static readonly KeywordData[] Keywords = new KeywordData[] {
                                                                               new KeywordData("v", KeywordType.GeometricVertex), new KeywordData("vn", KeywordType.VertexNormal), new KeywordData("vt", KeywordType.TextureVertex), new KeywordData("f", KeywordType.Face), new KeywordData("fo", KeywordType.Face), new KeywordData("g", KeywordType.Group), new KeywordData("l", KeywordType.Line), new KeywordData("o", KeywordType.ObjectName), new KeywordData("s", KeywordType.SmoothingGroup), new KeywordData("mtllib", KeywordType.MaterialLibrary), new KeywordData("usemtl", KeywordType.UseMaterial), new KeywordData("usemap", KeywordType.UseMap), new KeywordData("bevel", KeywordType.UnimplementedKeyword), new KeywordData("bmat", KeywordType.UnimplementedKeyword), new KeywordData("bsp", KeywordType.UnimplementedKeyword), new KeywordData("bzp", KeywordType.UnimplementedKeyword),
                                                                               new KeywordData("c_interop", KeywordType.UnimplementedKeyword), new KeywordData("cdc", KeywordType.UnimplementedKeyword), new KeywordData("con", KeywordType.UnimplementedKeyword), new KeywordData("cstype", KeywordType.UnimplementedKeyword), new KeywordData("ctech", KeywordType.UnimplementedKeyword), new KeywordData("curv", KeywordType.UnimplementedKeyword), new KeywordData("curv2", KeywordType.UnimplementedKeyword), new KeywordData("d_interop", KeywordType.UnimplementedKeyword), new KeywordData("deg", KeywordType.UnimplementedKeyword), new KeywordData("end", KeywordType.UnimplementedKeyword), new KeywordData("hole", KeywordType.UnimplementedKeyword), new KeywordData("lod", KeywordType.UnimplementedKeyword), new KeywordData("maplib", KeywordType.UnimplementedKeyword), new KeywordData("mg", KeywordType.UnimplementedKeyword), new KeywordData("p", KeywordType.UnimplementedKeyword), new KeywordData("param", KeywordType.UnimplementedKeyword),
                                                                               new KeywordData("parm", KeywordType.UnimplementedKeyword), new KeywordData("res", KeywordType.UnimplementedKeyword), new KeywordData("scrv", KeywordType.UnimplementedKeyword), new KeywordData("shadow_obj", KeywordType.UnimplementedKeyword), new KeywordData("sp", KeywordType.UnimplementedKeyword), new KeywordData("stech", KeywordType.UnimplementedKeyword), new KeywordData("step", KeywordType.UnimplementedKeyword), new KeywordData("surf", KeywordType.UnimplementedKeyword), new KeywordData("trace_obj", KeywordType.UnimplementedKeyword), new KeywordData("trim", KeywordType.UnimplementedKeyword), new KeywordData("vp", KeywordType.UnimplementedKeyword), new KeywordData("newmtl", KeywordType.NewMaterial), new KeywordData("illum", KeywordType.IlluminationMode), new KeywordData("Ka", KeywordType.Ambient), new KeywordData("Kd", KeywordType.Diffuse), new KeywordData("Ks", KeywordType.Specular),
                                                                               new KeywordData("Ke", KeywordType.Emissive), new KeywordData("d", KeywordType.Alpha), new KeywordData("Tf", KeywordType.Transparency), new KeywordData("Tr", KeywordType.Alpha), new KeywordData("Ns", KeywordType.Shininess), new KeywordData("Ni", KeywordType.RefractionIndex), new KeywordData("map_Ka", KeywordType.AmbientTextureFilename), new KeywordData("map_bump", KeywordType.BumpTextureFilename), new KeywordData("bump", KeywordType.BumpTextureFilename), new KeywordData("map_Kd", KeywordType.DiffuseTextureFilename), new KeywordData("map_Ks", KeywordType.SpecularTextureFilename), new KeywordData("map_Ns", KeywordType.ShininessTextureFilename), new KeywordData("map_refl", KeywordType.ReflectionTextureFilename), new KeywordData("refl", KeywordType.ReflectionTextureFilename), new KeywordData("sharpness", KeywordType.Sharpness)
                                                                           };
        // private IMessageLoggingService logger;
        //  private IMessageDisplayService manager;
        private int nextFaceIndex;
        // private SceneViewModel sceneViewModel;
        private static readonly char[] Separators = new char[] { ' ', '\t' };
        private Dictionary<int, List<int>> smoothingGroups;
        private List<Point> textureCoordinates;
        private int totalGeneratedVertices;
        private int totalVertices;
        private int totalVerticesInObj;
        private List<Vector3D> vertexNormals;



        //public WavefrontObjLoader(SceneViewModel sceneViewModel, IMessageLoggingService logger, IMessageDisplayService manager)
        //{
        //    this.sceneViewModel = sceneViewModel;
        //    this.logger = logger;
        //    this.manager = manager;
        //}
        private Dictionary<string, ModelVisual3DWithName> dictionaries;

        public WavefrontObjLoader()
        {
            dictionaries = new Dictionary<string, ModelVisual3DWithName>();
        }

        private GeometryModel3D BuildGeometryModel3DFromGeometry(Geometry geometry)
        {
            GeometryModel3D modeld = new GeometryModel3D();
            MeshGeometry3D geometryd = new MeshGeometry3D();
            SortedDictionary<UniqueVertex, int> dictionary = new SortedDictionary<UniqueVertex, int>(new UniqueVertexComparer());
            geometryd.TriangleIndices = new Int32Collection();
            bool flag = false;
            bool flag2 = false;
            Rect3D empty = Rect3D.Empty;
            for (int i = geometry.FirstFaceIndex; i <= geometry.LastFaceIndex; i++)
            {
                Face face = this.faces[i];
                int[] numArray = new int[face.CoordinateIndices.Count];
                for (int k = 0; k < face.CoordinateIndices.Count; k++)
                {
                    UniqueVertex key = new UniqueVertex();
                    key.Coordinate = this.coordinates[face.CoordinateIndices[k]];
                    if (face.NormalIndices != null)
                    {
                        key.Normal = this.vertexNormals[face.NormalIndices[k]];
                        flag = true;
                    }
                    if (face.TextureCoordinateIndices != null)
                    {
                        key.TextureCoordinates = this.textureCoordinates[face.TextureCoordinateIndices[k]];
                        flag2 = true;
                    }
                    int num3 = -1;
                    if (!dictionary.TryGetValue(key, out num3))
                    {
                        num3 = dictionary.Count;
                        dictionary.Add(key, num3);
                        empty.Union(key.Coordinate);
                    }
                    numArray[k] = num3;
                }
                for (int m = 1; m < (numArray.Length - 1); m++)
                {
                    geometryd.TriangleIndices.Add(numArray[0]);
                    geometryd.TriangleIndices.Add(numArray[m]);
                    geometryd.TriangleIndices.Add(numArray[m + 1]);
                }
            }
            int count = dictionary.Count;
            if (count == 0)
            {
                return null;
            }
            if (this.generateTextureCoordinates)
            {
                flag2 = true;
            }
            geometryd.Positions = new Point3DCollection(count);
            if (flag)
            {
                geometryd.Normals = new Vector3DCollection(count);
            }
            if (flag2)
            {
                geometryd.TextureCoordinates = new PointCollection(count);
            }
            Point3D center = empty.Location + new Vector3D(empty.SizeX / 2.0, empty.SizeY / 2.0, empty.SizeZ / 2.0);
            Point3D pointd2 = new Point3D();
            Vector3D vectord = new Vector3D();
            Point point = new Point();
            for (int j = 0; j < count; j++)
            {
                geometryd.Positions.Add(pointd2);
                if (flag)
                {
                    geometryd.Normals.Add(vectord);
                }
                if (flag2)
                {
                    geometryd.TextureCoordinates.Add(point);
                }
            }
            foreach (KeyValuePair<UniqueVertex, int> pair in dictionary)
            {
                geometryd.Positions[pair.Value] = pair.Key.Coordinate;
                if (pair.Key.NormalSet)
                {
                    geometryd.Normals[pair.Value] = pair.Key.Normal;
                }
                if (pair.Key.TextureCoordinatesSet)
                {
                    geometryd.TextureCoordinates[pair.Value] = pair.Key.TextureCoordinates;
                }
                else
                {
                    geometryd.TextureCoordinates[pair.Value] = GetPositionalSphericalTextureCoordinateFromPoint(pair.Key.Coordinate, center);
                }
            }
            modeld.Geometry = geometryd;
            modeld.Material = geometry.Material.Clone();
            //   modeld.SetValue(CommonProperties.NameProperty, SceneElementIDHelper.ToCSharpID(geometry.Name));
            return modeld;
        }

        private static Material CreateMaterial(ObjMaterial currentMaterial)
        {
            MaterialGroup group = new MaterialGroup();
            if (currentMaterial.AmbientTexture != null)
            {
                group.Children.Add(new EmissiveMaterial(new ImageBrush(currentMaterial.AmbientTexture)));
            }
            else
            {
                if (currentMaterial.Alpha < 1.0)
                {
                    currentMaterial.Ambient.ScA = (float)currentMaterial.Alpha;
                }
                group.Children.Add(new EmissiveMaterial(new SolidColorBrush(currentMaterial.Ambient)));
            }
            if (currentMaterial.DiffuseTexture != null)
            {
                group.Children.Add(new DiffuseMaterial(new ImageBrush(currentMaterial.DiffuseTexture)));
            }
            else
            {
                if (currentMaterial.Alpha < 1.0)
                {
                    currentMaterial.Diffuse.ScA = (float)currentMaterial.Alpha;
                }
                group.Children.Add(new DiffuseMaterial(new SolidColorBrush(currentMaterial.Diffuse)));
            }
            if (currentMaterial.IlluminationMode > 1)
            {
                if (currentMaterial.SpecularTexture != null)
                {
                    group.Children.Add(new SpecularMaterial(new ImageBrush(currentMaterial.SpecularTexture), currentMaterial.Shininess));
                    return group;
                }
                if (currentMaterial.Alpha < 1.0)
                {
                    currentMaterial.Specular.ScA = (float)currentMaterial.Alpha;
                }
                group.Children.Add(new SpecularMaterial(new SolidColorBrush(currentMaterial.Specular), currentMaterial.Shininess));
            }
            return group;
        }

        private static Material DefaultMaterial()
        {
            Random random = new Random();
            Material material = new DiffuseMaterial(new SolidColorBrush(Color.FromScRgb(1f, (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble())));
            return DefaultWhiteMaterial;
        }

        private bool FinishCurrentGeometry(Group group, Material material, string name, string materialName)
        {
            if (name == null)
            {
                name = materialName;
            }
            if ((this.faces.Count - 1) >= this.nextFaceIndex)
            {
                group.Geometry.Add(new Geometry(name, material, this.nextFaceIndex, this.faces.Count - 1));
                this.nextFaceIndex = this.faces.Count;
                return true;
            }
            return false;
        }

        private void FinishCurrentGroup(Group group)
        {
            if (group.Geometry.Count > 0)
            {
                this.groups.Add(group);
            }
        }

        private ModelVisual3DWithName GenerateAvalonTree()
        {
            var visuald = new ModelVisual3DWithName { Name = "RootGeometryContainer" };
            if (!dictionaries.ContainsKey(visuald.Name))
            {
                dictionaries.Add(visuald.Name, visuald);
            }

            for (int i = 0; i < this.groups.Count; i++)
            {
                Group group = this.groups[i];
                var visuald2 = new ModelVisual3DWithName { Name = group.Name };
                if (!dictionaries.ContainsKey(visuald2.Name))
                {
                    dictionaries.Add(visuald2.Name, visuald2);
                }

                if (group.Geometry.Count > 1)
                {
                    for (int j = 0; j < group.Geometry.Count; j++)
                    {
                        var visuald3 = new ModelVisual3DWithName
                        {
                            Name = group.Geometry[j].Name,
                            Content = BuildGeometryModel3DFromGeometry(
                                                                     group.Geometry[j])
                        };
                        if (visuald3.Content != null)
                        {
                            visuald2.Children.Add(visuald3);
                        }
                    }
                }
                else if (group.Geometry.Count == 1)
                {
                    visuald2.Content = BuildGeometryModel3DFromGeometry(group.Geometry[0]);
                }
                visuald.Children.Add(visuald2);
            }


            return visuald;
        }

        private static Point GetPositionalSphericalTextureCoordinateFromPoint(Point3D point, Point3D center)
        {
            Vector3D vectord = point - center;
            vectord.Normalize();
            return new Point((Math.Asin(vectord.X) / 3.1415926535897931) + 0.5, 1.0 - ((Math.Asin(vectord.Y) / 3.1415926535897931) + 0.5));
        }

        private Dictionary<string, ObjMaterial> LoadMtlFile(string mtlFilename)
        {
            Dictionary<string, ObjMaterial> materialLibrary = null;
            StreamReader reader;
            try
            {
                reader = new StreamReader(mtlFilename);
            }
            catch (FileNotFoundException)
            {
                return materialLibrary;
            }

            StreamReader reader2 = reader;
            try
            {
                ParseMtlFile(reader, Path.GetDirectoryName(mtlFilename), ref materialLibrary);
            }
            catch (InvalidOperationException exception)
            {
                //  this.manager.ShowError(message, StringTable.WavefrontObjAssetCreateElementFailedDialogTitle);
            }
            finally
            {
                if (reader2 != null)
                {
                    reader2.Dispose();
                }
            }
            return materialLibrary;
        }

        public ModelVisual3DWithName LoadObjFile(string targetFile)
        {
            using (Stream fs = new FileStream(targetFile, FileMode.Open, FileAccess.Read))
            {
                return LoadObjFile(new StreamReader(fs),
                                   Path.GetFullPath(targetFile).Replace(
                                   Path.GetFileName(targetFile), ""),
                                   Path.GetFileName(targetFile));
            }
        }



        public ModelVisual3DWithName LoadObjFile(StreamReader streamReader, string rootPath, string filename)
        {
            this.coordinates = new List<Point3D>();
            this.vertexNormals = new List<Vector3D>();
            this.textureCoordinates = new List<Point>();
            this.smoothingGroups = new Dictionary<int, List<int>>();
            this.faces = new List<Face>();
            this.groups = new List<Group>();
            this.nextFaceIndex = 0;
            this.totalVerticesInObj = 0;
            this.totalVertices = 0;
            this.totalGeneratedVertices = 0;
            Dictionary<string, ObjMaterial> dictionary = null;
            bool flag = false;
            string name = "DefaultGroup";
            string str2 = null;
            string materialName = "DefaultMaterial";
            int key = 0;
            Material material2 = DefaultMaterial();
            Group group = new Group(name);
            string str4 = string.Empty;
            while ((str4 = streamReader.ReadLine()) != null)
            {
                int length = str4.IndexOfAny(CommentCharacters);
                if (length != -1)
                {
                    str4 = str4.Substring(0, length);
                }
                int num3 = str4.IndexOfAny(Separators);
                string line = string.Empty;
                string content = string.Empty;
                if (str4.Trim().Length != 0)
                {
                    if (num3 != -1)
                    {
                        line = str4.Substring(0, num3);
                        while ((num3 < str4.Length) && ((str4[num3] == ' ') || (str4[num3] == '\t')))
                        {
                            num3++;
                        }
                        content = str4.Substring(num3);
                    }
                    else
                    {
                        line = str4;
                    }
                    switch (ParseKeyword(line))
                    {
                        case KeywordType.UnrecognizedKeyword:
                            {
                                continue;
                            }
                        case KeywordType.UnimplementedKeyword:
                            {
                                continue;
                            }
                        case KeywordType.Comment:
                        case KeywordType.Line:
                            {
                                continue;
                            }
                        case KeywordType.Face:
                            {
                                Face face;
                                if (!ParseFace(content, out face) || !this.NormalizeFaceIndices(face))
                                {
                                    goto Label_0301;
                                }
                                this.faces.Add(face);
                                if (key != 0)
                                {
                                    List<int> list;
                                    if (!this.smoothingGroups.TryGetValue(key, out list))
                                    {
                                        list = new List<int>(20);
                                        this.smoothingGroups.Add(key, list);
                                    }
                                    list.Add(this.faces.Count - 1);
                                }
                                continue;
                            }
                        case KeywordType.Group:
                            {
                                if (this.FinishCurrentGeometry(group, material2, str2, materialName))
                                {
                                    str2 = null;
                                }
                                this.FinishCurrentGroup(group);
                                content = content.TrimEnd(Separators);
                                if (content.Length != 0)
                                {
                                    name = content;
                                }
                                group = new Group(name);
                                continue;
                            }
                        case KeywordType.MaterialLibrary:
                            {
                                dictionary = LoadMtlFile(Path.Combine(rootPath, content));
                                continue;
                            }
                        case KeywordType.ObjectName:
                            {
                                str2 = content.TrimEnd(Separators);
                                continue;
                            }
                        case KeywordType.SmoothingGroup:
                            {
                                content = content.Trim(Separators);
                                if (!(content == "off"))
                                {
                                    goto Label_03EA;
                                }
                                key = 0;
                                continue;
                            }
                        case KeywordType.UseMaterial:
                            {
                                if (this.FinishCurrentGeometry(group, material2, str2, materialName))
                                {
                                    str2 = null;
                                }
                                if ((dictionary == null) && !flag)
                                {
                                    dictionary = this.LoadMtlFile(System.IO.Path.Combine(rootPath, filename.Replace(".obj", string.Empty) + ".mtl"));
                                    flag = true;
                                }
                                if (dictionary == null)
                                {
                                    goto Label_0207;
                                }
                                try
                                {
                                    ObjMaterial material = dictionary[content];
                                    material2 = material.Material;
                                    materialName = content;
                                }
                                catch (KeyNotFoundException)
                                {
                                    material2 = DefaultMaterial();
                                }
                                continue;
                            }
                        case KeywordType.GeometricVertex:
                            {
                                Point3D pointd;
                                if (!ParseVertex(content, out pointd))
                                {
                                    goto Label_0345;
                                }
                                this.totalVerticesInObj++;
                                this.coordinates.Add(pointd);
                                continue;
                            }
                        case KeywordType.VertexNormal:
                            {
                                Vector3D vectord;
                                if (!ParseVertexNormal(content, out vectord))
                                {
                                    goto Label_037B;
                                }
                                this.vertexNormals.Add(vectord);
                                continue;
                            }
                        case KeywordType.TextureVertex:
                            {
                                Point point;
                                if (!ParseVertexTextureCoordinate(content, out point))
                                {
                                    goto Label_03B1;
                                }
                                this.textureCoordinates.Add(point);
                                continue;
                            }
                    }
                }
                continue;
            Label_0207:;
                material2 = DefaultMaterial();
                continue;
            Label_0301:
                continue;
            Label_0345:
                continue;
            Label_037B:
                continue;
            Label_03B1:
                continue;
            Label_03EA:
                if (!int.TryParse(content, NumberStyles.Integer, CultureInfo.InvariantCulture, out key))
                {
                    //MessageBox.Show("string.Format(CultureInfo.CurrentCulture, StringTable.WavefrontObjLoaderErrorInvalidSmoothingGroup, new object[] { content })");
                }
            }
            this.FinishCurrentGeometry(group, material2, str2, materialName);
            this.FinishCurrentGroup(group);
            if (this.generateNormals)
            {
                foreach (KeyValuePair<int, List<int>> pair in this.smoothingGroups)
                {
                    List<int> list2 = pair.Value;
                    Dictionary<int, Vector3D> dictionary2 = new Dictionary<int, Vector3D>();
                    for (int i = 0; i < list2.Count; i++)
                    {
                        Face face2 = this.faces[list2[i]];
                        if (face2.NormalIndices == null)
                        {
                            for (int k = 0; k < face2.CoordinateIndices.Count; k++)
                            {
                                Vector3D normal;
                                if (!dictionary2.TryGetValue(face2.CoordinateIndices[k], out normal))
                                {
                                    normal = face2.Normal;
                                    dictionary2.Add(face2.CoordinateIndices[k], normal);
                                }
                                else
                                {
                                    normal += face2.Normal;
                                    dictionary2[face2.CoordinateIndices[k]] = normal;
                                }
                            }
                        }
                    }
                    Dictionary<int, int> dictionary3 = new Dictionary<int, int>();
                    foreach (KeyValuePair<int, Vector3D> pair2 in dictionary2)
                    {
                        Vector3D item = pair2.Value;
                        item.Normalize();
                        this.vertexNormals.Add(item);
                        dictionary3[pair2.Key] = this.vertexNormals.Count - 1;
                    }
                    for (int j = 0; j < list2.Count; j++)
                    {
                        Face face3 = this.faces[list2[j]];
                        if (face3.NormalIndices == null)
                        {
                            face3.NormalIndices = new List<int>(face3.CoordinateIndices.Count);
                            for (int m = 0; m < face3.CoordinateIndices.Count; m++)
                            {
                                face3.NormalIndices.Add(dictionary3[face3.CoordinateIndices[m]]);
                            }
                        }
                    }
                }
            }
            ModelVisual3DWithName visuald = this.GenerateAvalonTree();
            return visuald;
        }

        public ModelVisual3DWithName Find(string name)
        {
            ModelVisual3DWithName model;
            if (dictionaries.TryGetValue(name, out model))
            {
                return model;
            }

            return null;
        }

        public void Add(string name, ModelVisual3DWithName modelVisual3D)
        {
            if (Find(name) == null)
            {
                dictionaries.Add(name, modelVisual3D);
            }
        }

        public void Remove(string name)
        {
            if (Find(name) != null)
            {
                dictionaries.Remove(name);
            }
        }

        private BitmapImage LoadTexture(string rootPath, string content)
        {
            BitmapImage image = null;
            if (content[0] == '-')
            {
                content.IndexOfAny(Separators);
                content = content.Substring(content.LastIndexOfAny(Separators)).TrimStart(Separators);
            }
            string path = System.IO.Path.Combine(rootPath, content);
            if (File.Exists(path))
            {

                image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                image.EndInit();
            }
            return image;
        }

        private bool NormalizeFaceIndices(Face face)
        {
            for (int i = 0; i < face.CoordinateIndices.Count; i++)
            {
                if (face.CoordinateIndices[i] < 0)
                {
                    List<int> list;
                    int num2;
                    (list = face.CoordinateIndices)[num2 = i] = list[num2] + this.coordinates.Count;
                }
                else
                {
                    List<int> list2;
                    int num3;
                    (list2 = face.CoordinateIndices)[num3 = i] = list2[num3] - 1;
                }
                if ((face.CoordinateIndices[i] >= this.coordinates.Count) || (face.CoordinateIndices[i] < 0))
                {
                    return false;
                }
                if (face.NormalIndices != null)
                {
                    if (face.NormalIndices[i] < 0)
                    {
                        List<int> list3;
                        int num4;
                        (list3 = face.NormalIndices)[num4 = i] = list3[num4] + this.vertexNormals.Count;
                    }
                    else
                    {
                        List<int> list4;
                        int num5;
                        (list4 = face.NormalIndices)[num5 = i] = list4[num5] - 1;
                    }
                    if ((face.NormalIndices[i] >= this.vertexNormals.Count) || (face.NormalIndices[i] < -1))
                    {
                        face.NormalIndices = null;
                    }
                }
                if (face.TextureCoordinateIndices != null)
                {
                    if (face.TextureCoordinateIndices[i] < 0)
                    {
                        List<int> list5;
                        int num6;
                        (list5 = face.TextureCoordinateIndices)[num6 = i] = list5[num6] + this.textureCoordinates.Count;
                    }
                    else
                    {
                        List<int> list6;
                        int num7;
                        (list6 = face.TextureCoordinateIndices)[num7 = i] = list6[num7] - 1;
                    }
                    if ((face.TextureCoordinateIndices[i] >= this.textureCoordinates.Count) || (face.TextureCoordinateIndices[i] < -1))
                    {
                        face.TextureCoordinateIndices = null;
                    }
                }
            }
            Vector3D vectord = this.coordinates[face.CoordinateIndices[1]] - this.coordinates[face.CoordinateIndices[0]];
            Vector3D vectord2 = this.coordinates[face.CoordinateIndices[2]] - this.coordinates[face.CoordinateIndices[0]];
            face.Normal = Vector3D.CrossProduct(vectord, vectord2);
            return true;
        }

        private static bool ParseColor(string content, out Color color)
        {
            float num;
            float num2;
            float num3;
            string[] strArray = content.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            if (strArray.Length < 3)
            {
                color = new Color();
                return false;
            }
            bool flag = true;
            flag &= float.TryParse(strArray[0], NumberStyles.Float, CultureInfo.InvariantCulture, out num);
            flag &= float.TryParse(strArray[1], NumberStyles.Float, CultureInfo.InvariantCulture, out num2);
            flag &= float.TryParse(strArray[2], NumberStyles.Float, CultureInfo.InvariantCulture, out num3);
            color = Color.FromScRgb(1f, num, num2, num3);
            return flag;
        }

        private static bool ParseFace(string content, out Face face)
        {
            face = new Face();
            face.CoordinateIndices = new List<int>();
            face.NormalIndices = new List<int>();
            face.TextureCoordinateIndices = new List<int>();
            foreach (string str in content.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
            {
                int num;
                int num2;
                int num3;
                if (!ParseVertexReference(str, out num, out num2, out num3))
                {
                    return false;
                }
                face.CoordinateIndices.Add(num);
                face.NormalIndices.Add(num3);
                face.TextureCoordinateIndices.Add(num2);
            }
            if (face.CoordinateIndices.Count < 3)
            {
                return false;
            }
            bool flag = face.TextureCoordinateIndices[0] != 0x7fffffff;
            bool flag2 = face.NormalIndices[0] != 0x7fffffff;
            for (int i = 0; i < face.CoordinateIndices.Count; i++)
            {
                if ((face.TextureCoordinateIndices[i] != 0x7fffffff) != flag)
                {
                    return false;
                }
                if ((face.NormalIndices[i] != 0x7fffffff) != flag2)
                {
                    return false;
                }
            }
            if (!flag)
            {
                face.TextureCoordinateIndices = null;
            }
            if (!flag2)
            {
                face.NormalIndices = null;
            }
            return true;
        }

        private static KeywordType ParseKeyword(string line)
        {
            if ((line[0] == '#') || (line[0] == '$'))
            {
                return KeywordType.Comment;
            }
            foreach (KeywordData data in Keywords)
            {
                if (string.Compare(line, data.Name, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return data.Type;
                }
            }
            return KeywordType.UnrecognizedKeyword;
        }

        private void ParseMtlFile(StreamReader streamReader, string rootPath, ref Dictionary<string, ObjMaterial> materialLibrary)
        {
            materialLibrary = new Dictionary<string, ObjMaterial>();
            ObjMaterial currentMaterial = new ObjMaterial();
            currentMaterial.Alpha = 1.0;
            string str = string.Empty;
            while ((str = streamReader.ReadLine()) != null)
            {
                str = str.TrimStart(Separators);
                int length = str.IndexOfAny(Separators);
                string line = string.Empty;
                string s = string.Empty;
                if (str.Trim().Length != 0)
                {
                    BitmapImage image;
                    if (length != -1)
                    {
                        line = str.Substring(0, length);
                        while ((length < str.Length) && ((str[length] == ' ') || (str[length] == '\t')))
                        {
                            length++;
                        }
                        s = str.Substring(length);
                    }
                    else
                    {
                        line = str;
                    }
                    switch (ParseKeyword(line))
                    {
                        case KeywordType.UnrecognizedKeyword:
                            {
                                continue;
                            }
                        case KeywordType.UnimplementedKeyword:
                        case KeywordType.Comment:
                        case KeywordType.Face:
                        case KeywordType.Group:
                        case KeywordType.MaterialLibrary:
                        case KeywordType.ObjectName:
                        case KeywordType.SmoothingGroup:
                        case KeywordType.UseMaterial:
                        case KeywordType.GeometricVertex:
                        case KeywordType.VertexNormal:
                        case KeywordType.TextureVertex:
                        case KeywordType.RefractionIndex:
                        case KeywordType.Sharpness:
                            {
                                continue;
                            }
                        case KeywordType.IlluminationMode:
                            {
                                int num2;
                                if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out num2))
                                {
                                    goto Label_015A;
                                }
                                currentMaterial.IlluminationMode = num2;
                                continue;
                            }
                        case KeywordType.Alpha:
                            {
                                double num3;
                                if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out num3))
                                {
                                    goto Label_0192;
                                }
                                currentMaterial.Alpha = num3;
                                continue;
                            }
                        case KeywordType.Ambient:
                            {
                                Color color2;
                                if (!ParseColor(s, out color2))
                                {
                                    goto Label_01C0;
                                }
                                currentMaterial.Ambient = color2;
                                continue;
                            }
                        case KeywordType.Diffuse:
                            {
                                Color color3;
                                if (!ParseColor(s, out color3))
                                {
                                    goto Label_01EE;
                                }
                                currentMaterial.Diffuse = color3;
                                continue;
                            }
                        case KeywordType.Specular:
                            {
                                Color color4;
                                if (!ParseColor(s, out color4))
                                {
                                    goto Label_02A8;
                                }
                                currentMaterial.Specular = color4;
                                continue;
                            }
                        case KeywordType.Emissive:
                            {
                                Color color5;
                                if (!ParseColor(s, out color5))
                                {
                                    goto Label_02D6;
                                }
                                currentMaterial.Emissive = color5;
                                continue;
                            }
                        case KeywordType.AmbientTextureFilename:
                            {
                                image = this.LoadTexture(rootPath, s);
                                if (image != null)
                                {
                                    currentMaterial.AmbientTextureFilename = s;
                                    currentMaterial.AmbientTexture = image;
                                }
                                continue;
                            }
                        case KeywordType.BumpTextureFilename:
                            {
                                image = this.LoadTexture(rootPath, s);
                                if (image != null)
                                {
                                    currentMaterial.BumpTextureFilename = s;
                                    currentMaterial.BumpTexture = image;
                                }
                                continue;
                            }
                        case KeywordType.DiffuseTextureFilename:
                            {
                                image = this.LoadTexture(rootPath, s);
                                if (image != null)
                                {
                                    currentMaterial.DiffuseTextureFilename = s;
                                    currentMaterial.DiffuseTexture = image;
                                }
                                continue;
                            }
                        case KeywordType.ReflectionTextureFilename:
                            {
                                image = this.LoadTexture(rootPath, s);
                                if (image != null)
                                {
                                    currentMaterial.ReflectionTextureFilename = s;
                                    currentMaterial.ReflectionTexture = image;
                                }
                                continue;
                            }
                        case KeywordType.SpecularTextureFilename:
                            {
                                image = this.LoadTexture(rootPath, s);
                                if (image != null)
                                {
                                    currentMaterial.SpecularTextureFilename = s;
                                    currentMaterial.SpecularTexture = image;
                                }
                                continue;
                            }
                        case KeywordType.ShininessTextureFilename:
                            {
                                image = this.LoadTexture(rootPath, s);
                                if (image != null)
                                {
                                    currentMaterial.ShininessTextureFilename = s;
                                    currentMaterial.ShininessTexture = image;
                                }
                                continue;
                            }
                        case KeywordType.NewMaterial:
                            {
                                if ((currentMaterial.MaterialName != null) && (currentMaterial.Material == null))
                                {
                                    currentMaterial.Material = CreateMaterial(currentMaterial);
                                    materialLibrary[currentMaterial.MaterialName] = currentMaterial;
                                }
                                currentMaterial = new ObjMaterial();
                                currentMaterial.Alpha = 1.0;
                                currentMaterial.MaterialName = s;
                                continue;
                            }
                        case KeywordType.Shininess:
                            {
                                double num4;
                                if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out num4))
                                {
                                    goto Label_027A;
                                }
                                currentMaterial.Shininess = num4;
                                continue;
                            }
                        case KeywordType.Transparency:
                            {
                                Color color;
                                ParseColor(s, out color);
                                continue;
                            }
                    }
                }
                continue;
            Label_015A:
                continue;
            Label_0192:
                continue;
            Label_01C0:
                continue;
            Label_01EE:
                continue;
            Label_027A:
                continue;
            Label_02A8:
                continue;
            Label_02D6:
                ;

            }
            if ((currentMaterial.MaterialName != null) && (currentMaterial.Material == null))
            {
                currentMaterial.Material = CreateMaterial(currentMaterial);
                materialLibrary[currentMaterial.MaterialName] = currentMaterial;
            }
        }

        private static bool ParseVertex(string content, out Point3D vertex)
        {
            float num;
            float num2;
            float num3;
            string[] strArray = content.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            if (strArray.Length < 3)
            {
                vertex = new Point3D();
                return false;
            }
            bool flag = true;
            flag &= float.TryParse(strArray[0], NumberStyles.Float, CultureInfo.InvariantCulture, out num);
            flag &= float.TryParse(strArray[1], NumberStyles.Float, CultureInfo.InvariantCulture, out num2);
            flag &= float.TryParse(strArray[2], NumberStyles.Float, CultureInfo.InvariantCulture, out num3);
            vertex = new Point3D((double)num, (double)num2, (double)num3);
            return flag;
        }

        private static bool ParseVertexNormal(string content, out Vector3D vector)
        {
            float num;
            float num2;
            float num3;
            string[] strArray = content.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            if (strArray.Length < 3)
            {
                vector = new Vector3D();
                return false;
            }
            bool flag = true;
            flag &= float.TryParse(strArray[0], NumberStyles.Float, CultureInfo.InvariantCulture, out num);
            flag &= float.TryParse(strArray[1], NumberStyles.Float, CultureInfo.InvariantCulture, out num2);
            flag &= float.TryParse(strArray[2], NumberStyles.Float, CultureInfo.InvariantCulture, out num3);
            vector = new Vector3D((double)num, (double)num2, (double)num3);
            return flag;
        }

        private static bool ParseVertexReference(string content, out int geometricVertexIndex, out int textureVertexIndex, out int vertexNormalIndex)
        {
            string[] strArray = content.Split(new char[] { '/' });
            bool flag = false;
            geometricVertexIndex = 0x7fffffff;
            textureVertexIndex = 0x7fffffff;
            vertexNormalIndex = 0x7fffffff;
            if (strArray.Length > 0)
            {
                flag = int.TryParse(strArray[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out geometricVertexIndex);
            }
            if ((strArray.Length > 1) && (strArray[1].Length > 0))
            {
                flag &= int.TryParse(strArray[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out textureVertexIndex);
            }
            if ((strArray.Length > 2) && (strArray[2].Length > 0))
            {
                flag &= int.TryParse(strArray[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out vertexNormalIndex);
            }
            return flag;
        }

        private static bool ParseVertexTextureCoordinate(string content, out Point textureCoordinate)
        {
            float num;
            float num2;
            string[] strArray = content.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            if (strArray.Length < 2)
            {
                textureCoordinate = new Point();
                return false;
            }
            bool flag = true;
            flag &= float.TryParse(strArray[0], NumberStyles.Float, CultureInfo.InvariantCulture, out num);
            flag &= float.TryParse(strArray[1], NumberStyles.Float, CultureInfo.InvariantCulture, out num2);
            textureCoordinate = new Point((double)num, (double)(1f - num2));
            return flag;
        }

        private class Face
        {
            private List<int> coordinateIndices;
            private Vector3D normal;
            private List<int> normalIndices;
            private List<int> textureCoordinateIndices;

            public List<int> CoordinateIndices
            {
                get
                {
                    return this.coordinateIndices;
                }
                set
                {
                    this.coordinateIndices = value;
                }
            }

            public Vector3D Normal
            {
                get
                {
                    return this.normal;
                }
                set
                {
                    this.normal = value;
                }
            }

            public List<int> NormalIndices
            {
                get
                {
                    return this.normalIndices;
                }
                set
                {
                    this.normalIndices = value;
                }
            }

            public List<int> TextureCoordinateIndices
            {
                get
                {
                    return this.textureCoordinateIndices;
                }
                set
                {
                    this.textureCoordinateIndices = value;
                }
            }
        }

        private class Geometry
        {
            private int firstFaceIndex;
            private int lastFaceIndex;
            private System.Windows.Media.Media3D.Material material;
            private string name;

            public Geometry(string name, System.Windows.Media.Media3D.Material material, int firstFaceIndex, int lastFaceIndex)
            {
                this.name = name;
                this.material = material;
                this.firstFaceIndex = firstFaceIndex;
                this.lastFaceIndex = lastFaceIndex;
            }

            public int FirstFaceIndex
            {
                get
                {
                    return this.firstFaceIndex;
                }
            }

            public int LastFaceIndex
            {
                get
                {
                    return this.lastFaceIndex;
                }
            }

            public System.Windows.Media.Media3D.Material Material
            {
                get
                {
                    return this.material;
                }
                set
                {
                    this.material = value;
                }
            }

            public string Name
            {
                get
                {
                    return this.name;
                }
                set
                {
                    this.name = value;
                }
            }
        }

        private class Group
        {
            private List<WavefrontObjLoader.Geometry> geometry = new List<WavefrontObjLoader.Geometry>();
            private string name;

            public Group(string name)
            {
                this.name = name;
            }

            public List<WavefrontObjLoader.Geometry> Geometry
            {
                get
                {
                    return this.geometry;
                }
            }

            public string Name
            {
                get
                {
                    return this.name;
                }
                set
                {
                    this.name = value;
                }
            }
        }

        //   [StructLayout(LayoutKind.Sequential)]
        private struct KeywordData
        {
            public string Name;
            public WavefrontObjLoader.KeywordType Type;
            public KeywordData(string name, WavefrontObjLoader.KeywordType type)
            {
                this.Name = name;
                this.Type = type;
            }
        }

        private enum KeywordType
        {
            InvalidKeyword,
            UnrecognizedKeyword,
            UnimplementedKeyword,
            Comment,
            Face,
            Group,
            MaterialLibrary,
            ObjectName,
            SmoothingGroup,
            UseMaterial,
            GeometricVertex,
            VertexNormal,
            TextureVertex,
            IlluminationMode,
            Alpha,
            Ambient,
            Diffuse,
            Specular,
            Emissive,
            AmbientTextureFilename,
            BumpTextureFilename,
            DiffuseTextureFilename,
            ReflectionTextureFilename,
            SpecularTextureFilename,
            ShininessTextureFilename,
            NewMaterial,
            RefractionIndex,
            Shininess,
            Sharpness,
            Transparency,
            Point,
            Line,
            Curve,
            Curve2D,
            Surface,
            MergingGroup,
            Call,
            UseMap
        }

        // [StructLayout(LayoutKind.Sequential)]
        private struct ObjMaterial
        {
            public int IlluminationMode;
            public Color Ambient;
            public Color Diffuse;
            public Color Specular;
            public Color Emissive;
            public double Alpha;
            public double Shininess;
            public string MaterialName;
            public string AmbientTextureFilename;
            public string BumpTextureFilename;
            public string DiffuseTextureFilename;
            public string ReflectionTextureFilename;
            public string SpecularTextureFilename;
            public string ShininessTextureFilename;
            public BitmapImage AmbientTexture;
            public BitmapImage BumpTexture;
            public BitmapImage DiffuseTexture;
            public BitmapImage ReflectionTexture;
            public BitmapImage SpecularTexture;
            public BitmapImage ShininessTexture;
            public System.Windows.Media.Media3D.Material Material;
        }

        private class UniqueVertex
        {
            private Point3D coordinate;
            private Vector3D normal;
            private bool normalSet;
            private Point textureCoordinates;
            private bool textureCoordinatesSet;

            public int CompareWithinTolerance(WavefrontObjLoader.UniqueVertex other, double tolerance)
            {
                if (other.normalSet != this.normalSet)
                {
                    return other.normalSet.CompareTo(this.normalSet);
                }
                if (other.textureCoordinatesSet != this.textureCoordinatesSet)
                {
                    return other.textureCoordinatesSet.CompareTo(this.textureCoordinatesSet);
                }
                if (!DoubleIsWithinToleranceOf(other.coordinate.X, this.coordinate.X, tolerance))
                {
                    return other.coordinate.X.CompareTo(this.coordinate.X);
                }
                if (!DoubleIsWithinToleranceOf(other.coordinate.Y, this.coordinate.Y, tolerance))
                {
                    return other.coordinate.Y.CompareTo(this.coordinate.Y);
                }
                if (!DoubleIsWithinToleranceOf(other.coordinate.Z, this.coordinate.Z, tolerance))
                {
                    return other.coordinate.Z.CompareTo(this.coordinate.Z);
                }
                if (this.normalSet)
                {
                    if (!DoubleIsWithinToleranceOf(other.normal.X, this.normal.X, tolerance))
                    {
                        return other.normal.X.CompareTo(this.normal.X);
                    }
                    if (!DoubleIsWithinToleranceOf(other.normal.Y, this.normal.Y, tolerance))
                    {
                        return other.normal.Y.CompareTo(this.normal.Y);
                    }
                    if (!DoubleIsWithinToleranceOf(other.normal.Z, this.normal.Z, tolerance))
                    {
                        return other.normal.Z.CompareTo(this.normal.X);
                    }
                }
                if (this.textureCoordinatesSet)
                {
                    if (!DoubleIsWithinToleranceOf(other.textureCoordinates.X, this.textureCoordinates.X, tolerance))
                    {
                        return other.textureCoordinates.X.CompareTo(this.textureCoordinates.X);
                    }
                    if (!DoubleIsWithinToleranceOf(other.textureCoordinates.Y, this.textureCoordinates.Y, tolerance))
                    {
                        return other.textureCoordinates.Y.CompareTo(this.textureCoordinates.Y);
                    }
                }
                return 0;
            }

            private static bool DoubleIsWithinToleranceOf(double lhs, double rhs, double tolerance)
            {
                return (Math.Abs((double)(lhs - rhs)) < tolerance);
            }

            public Point3D Coordinate
            {
                get
                {
                    return this.coordinate;
                }
                set
                {
                    this.coordinate = value;
                }
            }

            public Vector3D Normal
            {
                get
                {
                    return this.normal;
                }
                set
                {
                    this.normal = value;
                    this.normalSet = true;
                }
            }

            public bool NormalSet
            {
                get
                {
                    return this.normalSet;
                }
            }

            public Point TextureCoordinates
            {
                get
                {
                    return this.textureCoordinates;
                }
                set
                {
                    this.textureCoordinates = value;
                    this.textureCoordinatesSet = true;
                }
            }

            public bool TextureCoordinatesSet
            {
                get
                {
                    return this.textureCoordinatesSet;
                }
            }
        }

        private class UniqueVertexComparer : Comparer<UniqueVertex>
        {
            public override int Compare(WavefrontObjLoader.UniqueVertex x, WavefrontObjLoader.UniqueVertex y)
            {
                return x.CompareWithinTolerance(y, WavefrontObjLoader.GeneratedVertexTolerance);
            }
        }
    }
}
