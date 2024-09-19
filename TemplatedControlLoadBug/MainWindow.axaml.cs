using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using HarmonyLib;

namespace TemplatedControlLoadBug;

public partial class MainWindow : Window
{
    private Harmony? _harmony;
    
    public MainWindow()
    {
        _harmony = new Harmony("DEADBEEF");
        var bitmap = new Bitmap(AssetLoader.Open(new Uri("avares://TemplatedControlLoadBug/578310.jpg")));
        Resources["TestImage"] = bitmap;
        InitializeComponent();
        HookStyleLoading(Assembly.GetAssembly(GetType())!);
    }

    private void HookStyleLoading(Assembly assembly)
    {
        var avaloniaResourcesType = assembly.GetType("CompiledAvaloniaXaml.!AvaloniaResources");
        var originalMethod = AccessTools.Method(avaloniaResourcesType, "Build:/TestingStyle.axaml");
        var newMethod = SymbolExtensions.GetMethodInfo((MethodInfo __originalMethod, Styles __result) =>
            HandleStyleLoadOverride(__originalMethod, ref __result));

        try
        {
            _harmony!.Patch(originalMethod, postfix: new HarmonyMethod(newMethod));
        }
        catch (SystemException e)
        {
            Console.WriteLine($"Error while hooking style: {e}");
        }
    }
    
    private static void HandleStyleLoadOverride(MethodInfo __originalMethod, ref Styles __result)
    {
        var assembly = __originalMethod.Module.Assembly;
        var avaUri = new Uri("avares://TemplatedControlLoadBug/TestingStyle.axaml");
        __result = (Styles)AvaloniaRuntimeXamlLoader.Load(changedStyleData, assembly, __result, avaUri);
    }
    
    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        var axamlData = @"<UserControl xmlns=""https://github.com/avaloniaui""
             xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
             xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
             xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
             xmlns:templatedControlLoadBug=""clr-namespace:TemplatedControlLoadBug""
             mc:Ignorable=""d"" d:DesignWidth=""800"" d:DesignHeight=""450"">
                <UserControl.Styles>
                    <StyleInclude Source=""TestingStyle.axaml"" />
                </UserControl.Styles>
                <templatedControlLoadBug:MagicTemplatedControl/>
            </UserControl>
        ";
        
        var assembly = Assembly.GetAssembly(GetType());
        var uri = new Uri("avares://TemplatedControlLoadBug/TestingControl.axaml");
        var obj = AvaloniaRuntimeXamlLoader.Load(axamlData, assembly, MagicContent.Child, uri) as Control;
        MagicContent.Child = obj;
    }
    
    private static readonly string changedStyleData = """
      <Styles xmlns="https://github.com/avaloniaui"
              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
              xmlns:uiTestContent="clr-namespace:TemplatedControlLoadBug">
          <Style Selector="uiTestContent|MagicTemplatedControl">
              <Style Selector="^ /template/ Button">
                  <Setter Property="Background">
                      <ImageBrush Source="{DynamicResource TestImage}"></ImageBrush><!-- This does not work -->
                  </Setter>
                  <Setter Property="BorderBrush">
                      <SolidColorBrush Color="Blue"/>
                  </Setter>
                  <Setter Property="Width" Value="150"/>
                  <Setter Property="Height" Value="40"/>
              </Style>
          </Style>
          
          <Style Selector="Panel">
              <Setter Property="Background">
                  <ImageBrush Source="{DynamicResource TestImage}"></ImageBrush>
              </Setter>
          </Style>
      
          <Style Selector="uiTestContent|MagicTemplatedControl">
              <Setter Property="Template">
                  <ControlTemplate>
                      <Border BorderThickness="2" BorderBrush="Red">
                          <StackPanel>
                              <Button>HUEBR</Button>
                              <ContentPresenter Content="{TemplateBinding Content}" />
                          </StackPanel>
                      </Border>
                  </ControlTemplate>
              </Setter>
          </Style>
      </Styles>
      """;
}