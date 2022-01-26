using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace marching_2d
{
  public sealed class MainWindow : Form
  {
    private const int imageWidth = 1000;
    private const int imageHeight = 1000;

    private const PixelFormat imageFormat = PixelFormat.Format32bppRgb;

    private readonly Bitmap imageRectangles = new(imageWidth, imageHeight, imageFormat);
    private readonly Bitmap imageTriangles = new(imageWidth, imageHeight, imageFormat);
    private readonly Bitmap imageDual = new(imageWidth, imageHeight, imageFormat);

    private const float xScale = 5f;
    private const float xOffset = 0f;
    private const float yScale = 5f;
    private const float yOffset = 0f;

    private const float rXScale = 1.0f / xScale;
    private const float rYScale = 1.0f / yScale;

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
      SetupUi();

      fastNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
      fastNoise.SetSeed(seed);

      for (int y = 0; y < imageHeight; ++y)
        for (int x = 0; x < imageWidth; ++x)
        {
          float v = FieldValueAt(x, y);
          float n = clamp(0.5f + 5f * v, 0f, 1f);
          int b = (int) (255f * n);

          imageRectangles.SetPixel(x, y, Color.FromArgb(b, b, b));
          imageTriangles.SetPixel(x, y, Color.FromArgb(b, b, b));
          imageDual.SetPixel(x, y, Color.FromArgb(b, b, b));
        }

      Pen pen = new Pen(Color.Fuchsia, 2);

      using (var graphics = Graphics.FromImage(imageRectangles))
        RenderRectangles(pen, graphics);

      using (var graphics = Graphics.FromImage(imageTriangles))
        RenderTriangles(pen, graphics);

      using (var graphics = Graphics.FromImage(imageDual))
        RenderDual(pen, graphics);
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
      RenderDual(Pen pen, Graphics graphics)
    {
      // throw new NotImplementedException();
    }

    private
      void
      RenderRectangles(Pen pen, Graphics graphics)
      =>
        MarchingRectangles.Draw(
          new MarchingRectangles.DrawParameters
          {
            pen = pen,
            graphics = graphics,
            imageWidth = imageWidth,
            imageHeight = imageHeight,
            gridWidth = 20,
            gridHeight = 20,
            fieldFunction = FieldValueAt
          });

    private
      void
      RenderTriangles(Pen pen, Graphics graphics)
      =>
        MarchingTriangles.Draw(
          new MarchingTriangles.DrawParameters
          {
            pen = pen,
            graphics = graphics,
            imageWidth = imageWidth,
            imageHeight = imageHeight,
            triangleSideLength = 50,
            fieldFunction = FieldValueAt
          });

    private
      void
      SetupUi()
    {
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(imageWidth * 3, imageHeight);
      FormBorderStyle = FormBorderStyle.FixedSingle;
      Text = @"Marching 2D";
      
      FlowLayoutPanel layout = new();
      layout.FlowDirection = FlowDirection.LeftToRight;
      layout.WrapContents = false;
      layout.Dock = DockStyle.Fill;
      layout.Padding = new Padding(0);
      layout.Margin = new Padding(0);
      
      var pictureBoxRectangles = new PictureBox();
      pictureBoxRectangles.Size = new Size(imageWidth, imageHeight);
      pictureBoxRectangles.Image = imageRectangles;
      pictureBoxRectangles.Dock = DockStyle.Left;
      pictureBoxRectangles.Margin = new Padding(0);
      // pictureBoxRectangles.Anchor = AnchorStyles.Left | AnchorStyles.Top;
      pictureBoxRectangles.SizeMode = PictureBoxSizeMode.Zoom;
      // Controls.Add(pictureBoxRectangles);
      layout.Controls.Add(pictureBoxRectangles);

      var pictureBoxDual = new PictureBox();
      pictureBoxDual.Size = new Size(imageWidth, imageHeight);
      pictureBoxDual.Image = imageDual;
      pictureBoxDual.Dock = DockStyle.Fill;
      pictureBoxDual.Anchor = AnchorStyles.None;
      pictureBoxDual.Margin = new Padding(0);
      // pictureBoxTriangles.Anchor = AnchorStyles.Right | AnchorStyles.Top;
      pictureBoxDual.SizeMode = PictureBoxSizeMode.Zoom;
      // Controls.Add(pictureBoxDual);
      layout.Controls.Add(pictureBoxDual);

      var pictureBoxTriangles = new PictureBox();
      pictureBoxTriangles.Size = new Size(imageWidth, imageHeight);
      pictureBoxTriangles.Image = imageTriangles;
      pictureBoxTriangles.Dock = DockStyle.Right;
      pictureBoxTriangles.Margin = new Padding(0);
      // pictureBoxTriangles.Anchor = AnchorStyles.Right | AnchorStyles.Top;
      pictureBoxTriangles.SizeMode = PictureBoxSizeMode.Zoom;
      // Controls.Add(pictureBoxTriangles);
      layout.Controls.Add(pictureBoxTriangles);
      
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