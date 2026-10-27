using System;using System.IO;using System.Windows;using System.Windows.Controls;using System.Windows.Media;using System.Windows.Media.Imaging;using CodexQuota;
class Demo {
 static Brush B(string color){return (Brush)new BrushConverter().ConvertFromString(color);}
 static void Text(DrawingContext d,string value,double x,double y,double size,string color,bool bold=false){var t=new FormattedText(value,System.Globalization.CultureInfo.GetCultureInfo("en-US"),FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe UI"),FontStyles.Normal,bold?FontWeights.SemiBold:FontWeights.Normal,FontStretches.Normal),size,B(color),1);d.DrawText(t,new Point(x,y));}
 static void Box(DrawingContext d,double x,double y,double w,double h,double radius,string fill,string stroke=null){d.DrawRoundedRectangle(B(fill),stroke==null?null:new Pen(B(stroke),1),new Rect(x,y,w,h),radius,radius);}
 [STAThread]static int Main(string[] args){
  var visual=new DrawingVisual();using(var d=visual.RenderOpen()){
   Box(d,0,0,1280,800,0,"#f8f7fa");Box(d,0,0,72,800,0,"#f0eef6");Box(d,72,48,310,752,16,"#fbfafc");Box(d,382,48,898,752,0,"#ffffff");
   Text(d,"‹       ›          ▥",20,8,23,"#8c8994");Text(d,"File       Edit       View       Help",175,15,14,"#77747d");
   Box(d,12,68,48,48,14,"#e5e3eb");Text(d,"⌂",25,73,28,"#222329",true);Text(d,"▧",26,145,22,"#8e8b98");Text(d,"◷",25,209,23,"#8e8b98");Text(d,"▥",25,272,22,"#8e8b98");Text(d,"◉",25,333,22,"#8e8b98");Text(d,"•••",25,398,18,"#8e8b98");
   d.DrawEllipse(B("#a75978"),null,new Point(36,773),17,17);Text(d,"A",30,762,15,"#ffffff",true);
   Text(d,"Codex",95,68,23,"#35343b",true);Text(d,"⌄",170,72,17,"#8e8b98");Text(d,"＋  New chat",95,129,18,"#45434d");
   Text(d,"Workspace",95,198,15,"#99949f");Text(d,"▱   Example project",95,235,17,"#55515c");Text(d,"     Explore ideas",95,281,16,"#55515c");Text(d,"     Draft a plan",95,325,16,"#55515c");Text(d,"Recent",95,391,15,"#99949f");Box(d,83,425,275,43,10,"#ecebef");Text(d,"Monitor my usage",95,435,16,"#4c4953");Text(d,"Example conversation",95,487,16,"#55515c");
   Text(d,"Monitor your Codex usage",410,70,20,"#292830",true);d.DrawLine(new Pen(B("#efedf1"),1),new Point(410,120),new Point(1240,120));
   Text(d,"A small view of your remaining usage",445,177,29,"#292830",true);Text(d,"The rings stay beside your profile and follow the window.",445,224,17,"#73707a");
   Box(d,445,285,690,138,16,"#f8f7fa","#e9e7ec");Text(d,"Your usage, at a glance",470,307,18,"#3c3944",true);Text(d,"Hover to see reset times. Right-click for options.",470,346,16,"#77717d");Text(d,"Values in this image are illustrative.",470,375,14,"#96919b");
   Box(d,445,650,730,72,22,"#ffffff","#e4e2e7");Text(d,"Message Codex…",472,670,17,"#bab7be");Text(d,"＋",472,728,24,"#78747e");
   var stack=new StackPanel{Margin=new Thickness(1,3,1,3)};stack.Children.Add(new Ring{Width=50,Height=58,Label="5h",Remaining=82,Stale=false});stack.Children.Add(new Ring{Width=50,Height=58,Label="wk",Remaining=67,Stale=false});
   var widget=new Border{Width=54,Height=126,CornerRadius=new CornerRadius(16),Background=B("#faf9fd"),BorderBrush=B("#e5e2ed"),BorderThickness=new Thickness(1),Child=stack};widget.Measure(new Size(54,126));widget.Arrange(new Rect(0,0,54,126));widget.UpdateLayout();d.DrawRectangle(new VisualBrush(widget),null,new Rect(9,594,54,126));
   Text(d,"DEMO · SAMPLE DATA",1025,14,12,"#9a96a0");
  }
  var bmp=new RenderTargetBitmap(1280,800,96,96,PixelFormats.Pbgra32);bmp.Render(visual);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bmp));using(var file=File.Create(args[0]))png.Save(file);return 0;
 }
}
