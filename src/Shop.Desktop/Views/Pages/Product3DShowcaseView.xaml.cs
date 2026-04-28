using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.SharpDX.Assimp;
using Shop.Desktop.Services;
using SharpDX;
using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Model.Scene;
using HelixToolkit.SharpDX.Model;
using HelixToolkit.Maths;

namespace Shop.Desktop.Views.Pages;

public partial class Product3DShowcaseView : UserControl
{
    private bool _isLoadedOnce;

    public Product3DShowcaseView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoadedOnce) return;
        _isLoadedOnce = true;

        var importer = new Importer();
        var scene = await Task.Run(() => importer.Load("Assets/Models/sofa.glb"));
        if (scene == null)
        {
            MessageBox.Show("模型加载失败（scene == null）");
            return;
        }
        //MessageBox.Show($"模型节点数: {scene.Root.Traverse().Count()}");
        var bounds = scene.Root.Bounds;
        //MessageBox.Show($"模型范围: {bounds.Minimum} - {bounds.Maximum}");

        foreach (var node in scene.Root.Traverse())
        {
            if (node is MeshNode mesh)
            {
                mesh.Material = new PhongMaterialCore
                {
                    DiffuseColor = new Color4(1f, 0f, 0f, 1f),   // 红色
                    AmbientColor = new Color4(0.2f, 0.2f, 0.2f, 1f),
                    SpecularColor = new Color4(1f, 1f, 1f, 1f),
                    SpecularShininess = 30f
                };
            }
        }
        view1.Items.Add(new Element3DPresenter { Content = scene.Root });
        view1.ZoomExtents();

    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        DesktopServices.Navigation.NavigateToHome();
    }
}

