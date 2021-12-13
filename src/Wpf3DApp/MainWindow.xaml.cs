using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Wpf3DApp
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        PerspectiveCamera myPCamera;
        DirectionalLight myDirectionalLight;
        Model3DGroup myModel3DGroup;
        WavefrontObjLoader wfl;
        ModelVisual3DWithName mv3dw;
        public MainWindow()
        {
            InitializeComponent();
            wfl = new WavefrontObjLoader();
            slider1.ValueChanged += new RoutedPropertyChangedEventHandler<double>(slider1_ValueChanged);
            slider2.ValueChanged += new RoutedPropertyChangedEventHandler<double>(slider2_ValueChanged);
            slider3.ValueChanged += new RoutedPropertyChangedEventHandler<double>(slider3_ValueChanged);
            slider4.ValueChanged += new RoutedPropertyChangedEventHandler<double>(slider4_ValueChanged);
            createCamera();
            createLight();
            createModel3D();
            create360();
            //createAnimation();
        }
        #region //光源
        private void createLight()
        {
            myDirectionalLight = new DirectionalLight();
            myDirectionalLight.Color = Colors.White;
            myDirectionalLight.Direction = new Vector3D(-0.61, -0.5, -0.61);
        }
        #endregion
        #region //摄像机
        private void createCamera()
        {
            myPCamera = new PerspectiveCamera();
            //myPCamera.Position = new Point3D(0,-1743,-4000);//看的方向
            myPCamera.Position = new Point3D(0, 0, 1000);
            myPCamera.LookDirection = new Vector3D(0, 0, -1000);//摄影机看的方向
            myPCamera.UpDirection = new Vector3D(0, 1, -0);
            myPCamera.FieldOfView = 45;//法向量摄影机上下颠倒,左转右转            
            myPCamera.NearPlaneDistance = 0.1;
            myPCamera.FarPlaneDistance = 11050;

            vp.Camera = myPCamera;
        }
        #endregion
        #region //模型
        private void createModel3D()
        {
            myModel3DGroup = new Model3DGroup();
            myModel3DGroup.Children.Add(myDirectionalLight);

            var m = wfl.LoadObjFile(@"D:\3d\oo.obj");//此处为obj文件的路径
            m.Content = myModel3DGroup;

            vp.Children.Add(m);
        }
        #endregion
        #region //360旋转动作
        RotateTransform3D rtf3D;
        AxisAngleRotation3D aar;
        private void create360()
        {
            rtf3D = new RotateTransform3D();
            aar = new AxisAngleRotation3D();
            this.RegisterName("myAngleRotation", aar);
            aar.Angle = 0;
            aar.Axis = new Vector3D(0, 3, 0);
            rtf3D.Rotation = aar;
            myModel3DGroup.Transform = rtf3D;
            myPCamera.Transform = rtf3D;
        }
        Storyboard sbd;
        DoubleAnimation dan;
        private void createAnimation()
        {
            sbd = new Storyboard();
            dan = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(10)));
            dan.RepeatBehavior = RepeatBehavior.Forever;
            Storyboard.SetTargetName(dan, "myAngleRotation");
            Storyboard.SetTargetProperty(dan, new PropertyPath(AxisAngleRotation3D.AngleProperty));
            sbd.Children.Add(dan);
            sbd.BeginTime = TimeSpan.FromSeconds(5);//开始时间
            sbd.Begin(this);
        }
        #endregion
        void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            myPCamera.Position = new Point3D(slider1.Value, slider2.Value, slider3.Value);
        }
        void slider2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            myPCamera.Position = new Point3D(slider1.Value, slider2.Value, slider3.Value);
        }
        void slider3_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            myPCamera.Position = new Point3D(slider1.Value, slider2.Value, slider3.Value);
        }
        void slider4_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            aar.Angle = slider4.Value;
        }

    }
}
