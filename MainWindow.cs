using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace marching_2d
{
  public sealed class MainWindow : Form
  {
    private const int
      imageWidth = 1000,
      imageHeight = 1000;

    private const int
      gridWidth = 50,
      gridHeight = 50;

    private const int
      triangleSideLength = 50;

    private const PixelFormat imageFormat = PixelFormat.Format32bppRgb;

    private delegate void IsosurfaceRenderer(RenderParameters rp);

    private readonly struct RendererAndImage
    {
      public readonly IsosurfaceRenderer renderer;
      public readonly Bitmap image;

      public RendererAndImage(IsosurfaceRenderer _renderer, Bitmap _image)
      {
        (renderer, image) = (_renderer, _image);
      }
    }

    private RendererAndImage[] renderersAndImages = null;

    private readonly Pen isopathPen = new(Color.Fuchsia, 2);
    private readonly Pen gridPen = new(Color.Chocolate, 1); // set null to not draw grids

    private const float
      xScale = 5f,
      xOffset = 0f,
      yScale = 5f,
      yOffset = 0f;

    private const float
      rXScale = 1.0f / xScale,
      rYScale = 1.0f / yScale;

    private readonly FastNoiseLite fastNoise = new();
    private const int seed = 1337;

    private static
      float
      clamp(float value, float min, float max)
    {
      return value < min ? min : value > max ? max : value;
    }

    public
      MainWindow()
    {
      SetupRenderersAndImages();
      SetupUi();

      fastNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
      fastNoise.SetSeed(seed);

      for (int y = 0; y < imageHeight; ++y)
        for (int x = 0; x < imageWidth; ++x)
        {
          float v = FieldValueAt(x, y);
          float n = clamp(0.5f + 5f * v, 0f, 1f);
          int b = (int) (255f * n);

          foreach (var rendererAndImage in renderersAndImages)
            rendererAndImage.image.SetPixel(x, y, Color.FromArgb(b, b, b));
        }

      RenderParameters renderParameters = new()
      {
        isopathPen = isopathPen,
        gridPen = gridPen,
        imageWidth = imageWidth,
        imageHeight = imageHeight
      };

      foreach (var rendererAndImage in renderersAndImages)
        using (renderParameters.graphics = Graphics.FromImage(rendererAndImage.image))
          rendererAndImage.renderer(renderParameters);
    }

    private
      float
      FieldValueAt(float x, float y)
    {
      const float primitiveScale = 100f;

      var noiseValue = fastNoise.GetNoise(x * rXScale + xOffset, y * rYScale + yOffset);

      float circleValue = 0f;
      {
        const float r = 100f / primitiveScale;

        // center circle in image
        var cx = (x - imageWidth / 2f) / primitiveScale;
        var cy = (y - imageHeight / 2f) / primitiveScale;

        circleValue = (float) Math.Tanh(r - Math.Sqrt(cx * cx + cy * cy));
      }

      float squareValue = 0f;
      {
        const float w = 250f;
        const float h = 50f;

        var sx = x - imageWidth / 2f;
        var sy = y - imageHeight / 2f;

        var dx = (w / 2f - Math.Abs(sx)) / primitiveScale;
        var dy = (h / 2f - Math.Abs(sy)) / primitiveScale;

        squareValue = (float) Math.Tanh(Math.Min(dx, dy));
      }

      float circleMinusSquare = Math.Min(circleValue, -squareValue);

      return Math.Max(noiseValue, circleMinusSquare);
    }

    private
      void
      SetupRenderersAndImages()
    {
      renderersAndImages = new RendererAndImage[]
      {
        new(
          rp => MarchingRectangles.Draw(rp, gridWidth, gridHeight, FieldValueAt),
          new Bitmap(imageWidth, imageHeight, imageFormat)),
        new(
          rp => DualContouring.Draw(rp, gridWidth, gridHeight, FieldValueAt),
          new Bitmap(imageWidth, imageHeight, imageFormat)),
        new(
          rp => MarchingTriangles.Draw(rp, triangleSideLength, FieldValueAt),
          new Bitmap(imageWidth, imageHeight, imageFormat))
      };
    }

    private
      void
      SetupUi()
    {
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(imageWidth * renderersAndImages.Length, imageHeight);
      FormBorderStyle = FormBorderStyle.FixedSingle;
      Text = @"Marching 2D";

      FlowLayoutPanel layout = new()
      {
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false,
        Dock = DockStyle.Fill,
        Padding = new Padding(0),
        Margin = new Padding(0)
      };
      foreach (var rendererAndImage in renderersAndImages)
        layout.Controls.Add(
          new PictureBox
          {
            Size = new Size(imageWidth, imageHeight),
            Image = rendererAndImage.image,
            Margin = new Padding(0),
            SizeMode = PictureBoxSizeMode.Zoom
          });
      Controls.Add(layout);
    }

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      SuspendLayout();
      ClientSize = new System.Drawing.Size(284, 261);
      Name = "MainWindow";
      ResumeLayout(false);
    }
  }
}