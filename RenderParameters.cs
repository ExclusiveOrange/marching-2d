using System.Drawing;

namespace marching_2d
{
  public struct RenderParameters
  {
    public Pen isopathPen;
    public Pen gridPen;
    public Graphics graphics;
    public int imageWidth;
    public int imageHeight;
  }
}