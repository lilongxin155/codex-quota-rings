using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CodexQuota {
static class Texts {
 static readonly bool Chinese=System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName=="zh";
 public static string T(string chinese,string english){return Chinese?chinese:english;}
}
// Window discovery is shared by the UI and the monitor without loading WPF.
static class AppWindows {
 [StructLayout(LayoutKind.Sequential)] struct RECT {public int left,top,right,bottom;}
 [DllImport("user32.dll")] static extern bool IsWindow(IntPtr window);
 [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr window,uint flags);
 [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr window,int index);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowTextLength(IntPtr window);
 [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window,out uint processId);
 [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
 [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr window,out RECT rect);
 delegate bool EnumWindowsProc(IntPtr window,IntPtr parameter);
 [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc callback,IntPtr parameter);
 [DllImport("kernel32.dll")] static extern IntPtr OpenProcess(uint access,bool inherit,uint processId);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern bool QueryFullProcessImageName(IntPtr process,uint flags,System.Text.StringBuilder name,ref uint size);
 [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
 [DllImport("user32.dll")] static extern bool IsIconic(IntPtr window);
 [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr window,uint relation);
 [DllImport("user32.dll")] static extern int GetSystemMetrics(int index);
 [StructLayout(LayoutKind.Sequential)] struct POINT {public int x,y;}
 [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr window,ref POINT point);
 [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window,out RECT rect);
 [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")] static extern int GetCloaked(IntPtr window,int attribute,out int value,int size);
 [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")] static extern int GetFrame(IntPtr window,int attribute,out RECT rect,int size);
 [DllImport("gdi32.dll")] static extern IntPtr CreateRectRgn(int left,int top,int right,int bottom);
 [DllImport("gdi32.dll")] static extern bool SetRectRgn(IntPtr region,int left,int top,int right,int bottom);
 [DllImport("gdi32.dll")] static extern int CombineRgn(IntPtr destination,IntPtr first,IntPtr second,int mode);
 [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr region);
 public static bool IsExposed(IntPtr window){
  int cloaked;if(window==IntPtr.Zero||!IsWindowVisible(window)||IsIconic(window)||(GetCloaked(window,14,out cloaked,4)==0&&cloaked!=0))return false;
  RECT client;POINT origin=new POINT();if(!GetClientRect(window,out client)||!ClientToScreen(window,ref origin))return false;
  int left=Math.Max(origin.x,GetSystemMetrics(76)),top=Math.Max(origin.y,GetSystemMetrics(77));
  int right=Math.Min(origin.x+client.right,GetSystemMetrics(76)+GetSystemMetrics(78)),bottom=Math.Min(origin.y+client.bottom,GetSystemMetrics(77)+GetSystemMetrics(79));
  if(left>=right||top>=bottom)return false;
  IntPtr exposed=CreateRectRgn(left,top,right,bottom),cover=CreateRectRgn(0,0,0,0);
  try{
   if(exposed==IntPtr.Zero||cover==IntPtr.Zero)return true;
   // Check Z order only on the monitor/refresh path, never in drag callbacks.
   // Layered windows may be translucent (including our rings); keep reading if unsure.
   for(IntPtr above=GetWindow(window,3);above!=IntPtr.Zero;above=GetWindow(above,3)){
    if(!IsWindowVisible(above)||IsIconic(above)||(GetWindowLong(above,-20)&0x80000)!=0||(GetCloaked(above,14,out cloaked,4)==0&&cloaked!=0))continue;
    RECT r;if(GetFrame(above,9,out r,16)!=0&&!GetWindowRect(above,out r))continue;
    if(r.right<=left||r.left>=right||r.bottom<=top||r.top>=bottom)continue;
    if(!SetRectRgn(cover,r.left,r.top,r.right,r.bottom))return true;
    int remaining=CombineRgn(exposed,exposed,cover,4);if(remaining==1)return false;if(remaining==0)return true;
   }
   return true;
  }finally{if(exposed!=IntPtr.Zero)DeleteObject(exposed);if(cover!=IntPtr.Zero)DeleteObject(cover);}
 }
 public static bool IsMain(IntPtr window){
  // Maximized/fullscreen windows may have no WS_CAPTION. Exclude tools/children.
  if(window==IntPtr.Zero||!IsWindow(window)||GetAncestor(window,2)!=window||(GetWindowLong(window,-20)&0x80)!=0||(GetWindowLong(window,-16)&0x40000000)!=0||GetWindowTextLength(window)==0)return false;
  uint pid;GetWindowThreadProcessId(window,out pid);IntPtr process=OpenProcess(0x1000,false,pid);if(process==IntPtr.Zero)return false;
  try{var name=new System.Text.StringBuilder(1024);uint size=(uint)name.Capacity;return QueryFullProcessImageName(process,0,name,ref size)&&Path.GetFileName(name.ToString()).Equals("ChatGPT.exe",StringComparison.OrdinalIgnoreCase);}finally{CloseHandle(process);}
 }
 public static IntPtr Find(){var foreground=GetForegroundWindow();if(IsMain(foreground))return foreground;IntPtr selected=IntPtr.Zero;long area=0;EnumWindows((window,p)=>{if(IsWindowVisible(window)&&IsMain(window)){RECT r;if(GetClientRect(window,out r)){long size=(long)r.right*r.bottom;if(size>area){area=size;selected=window;}}}return true;},IntPtr.Zero);return selected;}
}
static class Program {
 static void RunMonitor(){
  Process child=null;IntPtr parent=IntPtr.Zero;string directory=AppDomain.CurrentDomain.BaseDirectory;
  Action stop=()=>{if(child!=null){try{if(!child.HasExited){child.Kill();child.WaitForExit(1000);}}catch{}child.Dispose();child=null;}};
  AppDomain.CurrentDomain.ProcessExit+=(s,e)=>stop();
  // A sleeping monitor needs neither Application nor a WPF dispatcher.
  while(true){
   try{var candidate=File.Exists(Path.Combine(directory,"paused.flag"))?IntPtr.Zero:AppWindows.Find();if(!AppWindows.IsExposed(candidate)){bool wasRunning=child!=null;stop();parent=IntPtr.Zero;if(wasRunning)File.WriteAllText(Path.Combine(directory,"window-status.json"),"{\"checkedUtc\":\""+DateTime.UtcNow.ToString("o")+"\",\"target\":"+candidate.ToInt64()+",\"visible\":false,\"reason\":\"sleeping-not-on-screen\"}");}else{if(child!=null&&(child.HasExited||candidate!=parent))stop();if(child==null){parent=candidate;child=Process.Start(new ProcessStartInfo(Path.Combine(directory,"CodexQuotaWidget.exe"),"--attached"){UseShellExecute=false,CreateNoWindow=true});}}}
   catch(Exception e){try{File.WriteAllText(Path.Combine(directory,"monitor-error.txt"),DateTime.Now.ToString("s")+" "+e.Message);}catch{}}
   Thread.Sleep(500);
  }
 }
 // Keep WPF type resolution out of the monitor's startup/JIT path.
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 static void RunWidget(){RenderOptions.ProcessRenderMode=RenderMode.SoftwareOnly;new Application().Run(new Widget());}
 [STAThread] public static void Main(){var args=Environment.GetCommandLineArgs();bool attached=Array.IndexOf(args,"--attached")>=0;if(Array.IndexOf(args,"--resume")>=0){string flag=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"paused.flag");if(File.Exists(flag))File.Delete(flag);}bool first;using(var mutex=new Mutex(true,attached?"Local\\CodexQuotaWidget_1":"Local\\CodexQuotaWidget_Monitor",out first)){if(!first)return;try{if(attached)RunWidget();else RunMonitor();}catch(Exception e){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"fatal-error.txt"),e.ToString());}}}
}
class Rpc : IDisposable {
 Process process; IntPtr job; int serial; readonly object sync=new object();
 [StructLayout(LayoutKind.Sequential)] struct BasicLimit {public long processTime,jobTime; public uint flags; public UIntPtr minWorking,maxWorking;public uint active;public UIntPtr affinity;public uint priority,scheduling;}
 [StructLayout(LayoutKind.Sequential)] struct IoCounters {public ulong readOps,writeOps,otherOps,readBytes,writeBytes,otherBytes;}
 [StructLayout(LayoutKind.Sequential)] struct ExtendedLimit {public BasicLimit basic;public IoCounters io;public UIntPtr processMemory,jobMemory,peakProcessMemory,peakJobMemory;}
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr CreateJobObject(IntPtr attributes,string name);
 [DllImport("kernel32.dll")] static extern bool SetInformationJobObject(IntPtr job,int infoClass,ref ExtendedLimit info,uint length);
 [DllImport("kernel32.dll")] static extern bool AssignProcessToJobObject(IntPtr job,IntPtr process);
 [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
 readonly Dictionary<int,TaskCompletionSource<Dictionary<string,object>>> pending=new Dictionary<int,TaskCompletionSource<Dictionary<string,object>>>();
 public async Task Start() {
  string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"OpenAI","Codex","bin");
  string exe=Directory.GetDirectories(root).Select(d=>Path.Combine(d,"codex.exe")).Where(File.Exists).OrderByDescending(File.GetLastWriteTimeUtc).First();
  process=new Process { StartInfo=new ProcessStartInfo(exe,"app-server --listen stdio://") { UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=System.Text.Encoding.UTF8 } };
  process.OutputDataReceived+=(s,e)=> { if(e.Data==null) { Fail(); return; } try { var obj=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(e.Data); object id; if(obj.TryGetValue("id",out id)&&id!=null) { TaskCompletionSource<Dictionary<string,object>> t; lock(sync) { if(!pending.TryGetValue(Convert.ToInt32(id),out t))return; pending.Remove(Convert.ToInt32(id)); } t.TrySetResult(obj); } } catch {} };
  process.ErrorDataReceived+=(s,e)=>{};
  process.Start();
  job=CreateJobObject(IntPtr.Zero,null);var limit=new ExtendedLimit();limit.basic.flags=0x2000;
  if(job==IntPtr.Zero||!SetInformationJobObject(job,9,ref limit,(uint)Marshal.SizeOf(typeof(ExtendedLimit)))||!AssignProcessToJobObject(job,process.Handle))throw new IOException(Texts.T("无法建立额度进程的自动清理保护","Could not create process cleanup protection"));
  process.BeginOutputReadLine();process.BeginErrorReadLine();
  await Call("initialize",new {clientInfo=new {name="codex_quota_widget",title="Codex Quota Widget",version="1.0.0"}});
  process.StandardInput.WriteLine("{\"method\":\"initialized\"}");process.StandardInput.Flush();
 }
 public async Task<Dictionary<string,object>> Call(string method,object args) {
  int id=Interlocked.Increment(ref serial); var t=new TaskCompletionSource<Dictionary<string,object>>(TaskCreationOptions.RunContinuationsAsynchronously);
  lock(sync)pending[id]=t;
  try {
   process.StandardInput.WriteLine(new JavaScriptSerializer().Serialize(new {id=id,method=method,@params=args}));process.StandardInput.Flush();
   if(await Task.WhenAny(t.Task,Task.Delay(45000))!=t.Task)throw new TimeoutException(Texts.T("读取额度超时","Reading usage limits timed out"));
   var response=await t.Task; if(response.ContainsKey("error"))throw new Exception(Texts.T("Codex 接口暂不可用","Codex interface is unavailable"));
   return (Dictionary<string,object>)response["result"];
  } finally {lock(sync)pending.Remove(id);}
 }
 void Fail() {lock(sync) {foreach(var t in pending.Values)t.TrySetException(new IOException(Texts.T("Codex 连接已关闭","Codex connection closed")));pending.Clear();}}
 public void Dispose() {if(process!=null) {try {if(!process.HasExited){process.Kill();process.WaitForExit(1000);}}catch{}process.Dispose();process=null;}if(job!=IntPtr.Zero){CloseHandle(job);job=IntPtr.Zero;}Fail();}
}
class Ring : FrameworkElement {
 public double? Remaining; public string Label=""; public bool Stale=true;
 protected override void OnRender(DrawingContext d) {
  var center=new Point(25,23); double r=19;
  d.DrawEllipse(new SolidColorBrush(Color.FromRgb(250,249,253)),null,center,22,22);
  d.DrawEllipse(null,new Pen(new SolidColorBrush(Color.FromRgb(226,224,234)),3.2),center,r,r);
  Color color=Stale?Color.FromRgb(157,155,168):(Remaining<15?Color.FromRgb(213,84,73):(Remaining<35?Color.FromRgb(208,148,48):((Label=="周"||Label=="wk")?Color.FromRgb(132,105,208):Color.FromRgb(42,158,143))));
  if(Remaining.HasValue&&Remaining.Value>0) {
   if(Remaining>=99.99)d.DrawEllipse(null,new Pen(new SolidColorBrush(color),3.2),center,r,r);
   else {double angle=Remaining.Value/100*2*Math.PI;var geo=new StreamGeometry();using(var c=geo.Open()) {c.BeginFigure(new Point(25,23-r),false,false);c.ArcTo(new Point(25+r*Math.Sin(angle),23-r*Math.Cos(angle)),new Size(r,r),0,angle>Math.PI,SweepDirection.Clockwise,true,false);} var pen=new Pen(new SolidColorBrush(color),3.2){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};d.DrawGeometry(null,pen,geo);}
  }
  Draw(d,Remaining.HasValue?Math.Round(Remaining.Value).ToString():"—",14,FontWeights.SemiBold,new SolidColorBrush(Color.FromRgb(56,52,69)),23);
  Draw(d,Label+(Stale?" ·":""),9,FontWeights.Normal,new SolidColorBrush(Color.FromRgb(124,118,139)),51);
  if(Remaining.HasValue)Draw(d,"%",6,FontWeights.Normal,new SolidColorBrush(Color.FromRgb(124,118,139)),34);
 }
 void Draw(DrawingContext d,string s,double size,FontWeight weight,Brush brush,double y) {var f=new FormattedText(s,System.Globalization.CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface(new FontFamily("Microsoft YaHei UI"),FontStyles.Normal,weight,FontStretches.Normal),size,brush,VisualTreeHelper.GetDpi(this).PixelsPerDip);d.DrawText(f,new Point((50-f.Width)/2,y-f.Height/2));}
}
class Widget : Window {
 [StructLayout(LayoutKind.Sequential)] public struct RECT {public int left,top,right,bottom;}
 [StructLayout(LayoutKind.Sequential)] public struct POINT {public int x,y;}
 [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr w,out RECT r);
 [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr w,ref POINT p);
 [DllImport("user32.dll")] static extern bool IsIconic(IntPtr w);
 [DllImport("user32.dll")] static extern bool IsWindow(IntPtr w);
 [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr w,IntPtr after,int x,int y,int cx,int cy,uint flags);
 [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr w);
 [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr w,out uint processId);
 [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr w,uint flags);
 [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr w,uint command);
 [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr w);
 [DllImport("user32.dll",SetLastError=true)] static extern IntPtr SetParent(IntPtr child,IntPtr parent);
 [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr window);
 delegate void WinEventProc(IntPtr hook,uint eventType,IntPtr window,int objectId,int childId,uint threadId,uint eventTime);
 [DllImport("user32.dll")] static extern IntPtr SetWinEventHook(uint min,uint max,IntPtr module,WinEventProc callback,uint processId,uint threadId,uint flags);
 [DllImport("user32.dll")] static extern bool UnhookWinEvent(IntPtr hook);
 delegate bool EnumWindowsProc(IntPtr window,IntPtr parameter);
 [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc callback,IntPtr parameter);
 [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr w,int idx);
 [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr w,int idx,int val);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowTextLength(IntPtr w);
 readonly Ring shortRing=new Ring {Width=50,Height=58,Label="5h"},weekRing=new Ring {Width=50,Height=58,Label=Texts.T("周","wk")};
 readonly DispatcherTimer anchorTimer=new DispatcherTimer(),refreshTimer=new DispatcherTimer();
 readonly string directory=AppDomain.CurrentDomain.BaseDirectory;
 Rpc rpc; bool busy,closing,positioning,scanBusy; DateTime? updated; IntPtr target,handle; int offsetY=0; int anchorTicks=0; readonly bool diagnostic; int lastX=int.MinValue,lastY,lastWidth,lastHeight;IntPtr locationHook,stateHook,hookTarget;WinEventProc locationCallback,stateCallback;
 DateTime nextUpdateScan=DateTime.MinValue; Rect? updateButton; bool updateScanKnown; int updateScanFailures;
 public Widget(bool diagnostic=false) {
  this.diagnostic=diagnostic;
  Title=Texts.T("Codex 额度圆环","Codex Quota Rings");Width=54;Height=126;Left=-32000;Top=-32000;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;ShowInTaskbar=false;ShowActivated=false;
  var stack=new StackPanel {Margin=new Thickness(1,3,1,3)};stack.Children.Add(shortRing);stack.Children.Add(weekRing);
  var border=new Border {CornerRadius=new CornerRadius(16),Background=new SolidColorBrush(Color.FromArgb(247,250,249,253)),BorderBrush=new SolidColorBrush(Color.FromRgb(229,226,237)),BorderThickness=new Thickness(1),Child=stack};Content=border;SetTip(Texts.T("正在连接 Codex 额度接口…\n连接后每10秒刷新；右键可退出","Connecting to Codex usage limits…\nRefreshes every 10 seconds; right-click for options"));
  var menu=new ContextMenu();Add(menu,Texts.T("立即刷新","Refresh now"),async()=>await Refresh());Add(menu,Texts.T("向上移动","Move up"),()=>{offsetY-=20;SavePosition();Anchor();});Add(menu,Texts.T("向下移动","Move down"),()=>{offsetY=Math.Min(offsetY+20,30);SavePosition();Anchor();});Add(menu,Texts.T("恢复默认位置","Reset position"),()=>{offsetY=0;SavePosition();Anchor();});Add(menu,Texts.T("退出组件","Pause widget"),()=>{File.WriteAllText(Path.Combine(directory,"paused.flag"),"paused");Close();});border.ContextMenu=menu;
  border.MouseLeftButtonUp+=async(s,e)=>await Refresh();
  ToolTipService.SetInitialShowDelay(border,150);ToolTipService.SetShowDuration(border,30000);
  try {offsetY=int.Parse(File.ReadAllText(Path.Combine(directory,"position.txt")));}catch{}
  SourceInitialized+=(s,e)=> {handle=new WindowInteropHelper(this).Handle;HwndSource.FromHwnd(handle).CompositionTarget.RenderMode=RenderMode.SoftwareOnly;SetWindowLong(handle,-20,GetWindowLong(handle,-20)|0x08000000|0x80);};
  Loaded+=async(s,e)=> {if(diagnostic)return;anchorTimer.Interval=TimeSpan.FromMilliseconds(250);anchorTimer.Tick+=(a,b)=>{try{Anchor();}catch{Hide();target=IntPtr.Zero;}};anchorTimer.Start();refreshTimer.Interval=TimeSpan.FromSeconds(10);refreshTimer.Tick+=async(a,b)=>await Refresh();refreshTimer.Start();Anchor();await Refresh();};
  Closing+=(s,e)=>{closing=true;anchorTimer.Stop();refreshTimer.Stop();if(locationHook!=IntPtr.Zero)UnhookWinEvent(locationHook);if(stateHook!=IntPtr.Zero)UnhookWinEvent(stateHook);if(rpc!=null)rpc.Dispose();};
 }
 void SavePosition(){try{File.WriteAllText(Path.Combine(directory,"position.txt"),offsetY.ToString());}catch{}}
 static void Add(ContextMenu menu,string title,Action action){var item=new MenuItem {Header=title};item.Click+=(s,e)=>action();menu.Items.Add(item);}
 void Anchor() {
  if(closing)return;
  if(updated.HasValue&&(DateTime.UtcNow-updated.Value).TotalSeconds>150&&!shortRing.Stale){shortRing.Stale=weekRing.Stale=true;shortRing.InvalidateVisual();weekRing.InvalidateVisual();SetTip(Texts.T("数据已过期，等待刷新","Data is stale; waiting for a refresh"));WriteStaleStatus();}
  if(!diagnostic&&!IsMainAppWindow(target))target=FindMainAppWindow();
  if(target==IntPtr.Zero&&!busy&&rpc!=null){rpc.Dispose();rpc=null;}
  if(target==IntPtr.Zero||IsIconic(target)||!IsWindowVisible(target)) {if(IsVisible)Hide();if(++anchorTicks%20==0)try{File.WriteAllText(Path.Combine(directory,"window-status.json"),new JavaScriptSerializer().Serialize(new {checkedUtc=DateTime.UtcNow.ToString("o"),target=target.ToInt64(),widget=handle.ToInt64(),visible=false,reason=target==IntPtr.Zero?"waiting-for-app":"minimized-or-hidden"}));}catch{}return;}
  if(hookTarget!=target){AttachLocationHook(target);lastX=int.MinValue;updateScanKnown=false;updateButton=null;nextUpdateScan=DateTime.MinValue;}
  if(!diagnostic&&DateTime.UtcNow>=nextUpdateScan)BeginUpdateScan();
  SyncVisibilityAndOrder();
  // Diagnostics belong to the maintenance timer, never to a drag event.
  if(++anchorTicks%20==0)try{File.WriteAllText(Path.Combine(directory,"window-status.json"),new JavaScriptSerializer().Serialize(new {checkedUtc=DateTime.UtcNow.ToString("o"),target=target.ToInt64(),widget=handle.ToInt64(),owner=GetWindow(handle,4).ToInt64(),attachment="independent-window-lightweight-events",locationHook=locationHook.ToInt64(),visible=IsVisible,x=lastX,y=lastY,dpi=GetDpiForWindow(target),width=lastWidth,height=lastHeight,updateScanKnown=updateScanKnown,updateButtonDetected=updateButton.HasValue,layered=(GetWindowLong(handle,-20)&0x80000)!=0,renderMode=HwndSource.FromHwnd(handle).CompositionTarget.RenderMode.ToString()}));}catch{}
 }
 void SyncPosition() {
  if(closing||positioning||target==IntPtr.Zero||IsIconic(target))return;
  positioning=true;
  try {
   RECT r;POINT origin=new POINT();if(!GetClientRect(target,out r)||!ClientToScreen(target,ref origin))return;
   double scale=GetDpiForWindow(target)/96.0;if(scale<=0)scale=1;
   int x=origin.x+(int)(5*scale),y=origin.y+r.bottom-(int)((184-offsetY)*scale);
   y=Math.Max(origin.y+(int)(100*scale),Math.Min(y,origin.y+r.bottom-(int)(145*scale)));
   int width=(int)(54*scale),height=(int)(126*scale);
   if(!updateScanKnown){y-=(int)(64*scale);}
   else if(updateButton.HasValue){
    Rect button=updateButton.Value;var widgetRect=new Rect(x-origin.x,y-origin.y,width,height);
    button.Inflate(0,4*scale);
    if(widgetRect.IntersectsWith(button)){
     int above=origin.y+(int)Math.Floor(button.Top)-height-(int)(4*scale);
     if(above>=origin.y+(int)(100*scale))y=above;
     else x=origin.x+(int)(78*scale);
    }
   }
   bool resized=width!=lastWidth||height!=lastHeight;
   if(x==lastX&&y==lastY&&!resized)return;
   // Preserve owner Z order and suppress cross-thread WM_SYNCPAINT work.
   const uint flags=0x0010|0x0004|0x0200|0x2000;
   if(!SetWindowPos(handle,IntPtr.Zero,x,y,width,height,flags|(resized?0u:0x0001u)))return;
   lastX=x;lastY=y;lastWidth=width;lastHeight=height;
  }finally{positioning=false;}
 }
 void BeginUpdateScan(){
  if(scanBusy||closing||target==IntPtr.Zero)return;
  scanBusy=true;nextUpdateScan=DateTime.UtcNow.AddSeconds(5);IntPtr scannedTarget=target;
  Task.Run(()=>{
   bool ok=false;Rect? found=null;
   try{found=FindUpdateButton(scannedTarget);ok=true;}catch{}
   Dispatcher.BeginInvoke(new Action(()=>{scanBusy=false;if(closing||scannedTarget!=target)return;if(ok){updateScanKnown=true;updateScanFailures=0;updateButton=found;}else if(++updateScanFailures>=2){updateScanKnown=false;updateButton=null;}SyncPosition();}));
  });
 }
 static Rect? FindUpdateButton(IntPtr window){
  POINT before=new POINT();RECT client;if(!GetClientRect(window,out client)||!ClientToScreen(window,ref before))throw new InvalidOperationException();
  double scale=GetDpiForWindow(window)/96.0;if(scale<=0)scale=1;
  var root=AutomationElement.FromHandle(window);if(root==null)throw new InvalidOperationException();
  var kind=new OrCondition(new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Button),new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.ProgressBar));
  AutomationElementCollection elements;var cache=new CacheRequest();cache.Add(AutomationElement.NameProperty);cache.Add(AutomationElement.BoundingRectangleProperty);cache.Add(AutomationElement.IsOffscreenProperty);cache.AutomationElementMode=AutomationElementMode.None;
  using(cache.Activate())elements=root.FindAll(TreeScope.Descendants,kind);
  Rect? result=null;bool railFound=false;
  foreach(AutomationElement element in elements)try{
   var p=element.Cached;Rect b=p.BoundingRectangle;string name=p.Name??"";
   if(p.IsOffscreen||b.IsEmpty||b.Width<=0||b.Height<=0||b.Left<before.x||b.Left>=before.x+75*scale||b.Top<before.y||b.Bottom>before.y+client.bottom||b.Top<before.y+client.bottom-400*scale)continue;
   railFound=true;if(!IsUpdateLabel(name))continue;
   var relative=new Rect(b.X-before.x,b.Y-before.y,b.Width,b.Height);result=result.HasValue?Rect.Union(result.Value,relative):relative;
  }catch(ElementNotAvailableException){}
  POINT after=new POINT();if(!ClientToScreen(window,ref after)||after.x!=before.x||after.y!=before.y)throw new InvalidOperationException();
  // A newly started Chromium accessibility tree may initially expose only native title-bar controls.
  // Retry and reserve a safe gap until the navigation rail is available.
  if(!railFound)throw new InvalidOperationException();return result;
 }
 static bool IsUpdateLabel(string name){return name.IndexOf("update",StringComparison.OrdinalIgnoreCase)>=0||name.IndexOf("downloading",StringComparison.OrdinalIgnoreCase)>=0||name.IndexOf("installing",StringComparison.OrdinalIgnoreCase)>=0||name.Contains("更新")||name.Contains("下载")||name.Contains("下載")||name.Contains("安装")||name.Contains("安裝");}
 void SyncVisibilityAndOrder() {
  if(closing)return;
  if(target==IntPtr.Zero||!IsWindow(target)||IsIconic(target)||!IsWindowVisible(target)){if(IsVisible)Hide();return;}
  SyncPosition();if(!IsVisible)Show();
  IntPtr above=GetWindow(target,3);
  // Cross-process Owner/SetParent would attach input queues and stall dragging.
  // Place this independent window immediately above Codex instead.
  if(above!=handle)SetWindowPos(handle,above,0,0,0,0,0x0001|0x0002|0x0010|0x0200|0x2000);
 }
 void AttachLocationHook(IntPtr parent){
  if(locationHook!=IntPtr.Zero)UnhookWinEvent(locationHook);
  if(stateHook!=IntPtr.Zero)UnhookWinEvent(stateHook);
  uint pid;GetWindowThreadProcessId(parent,out pid);hookTarget=parent;
  locationCallback=(hook,type,window,obj,child,thread,time)=>{if(closing||window!=target||obj!=0||child!=0)return;try{if(type==0x800B)SyncPosition();else if(type==0x8000||type==0x8001||type==0x8002||type==0x8003||type==0x8004)SyncVisibilityAndOrder();}catch{}};
  stateCallback=(hook,type,window,obj,child,thread,time)=>{if(closing)return;if(type!=3&&window!=target)return;try{if(type==3||type==0x16||type==0x17)SyncVisibilityAndOrder();}catch{}};
  locationHook=SetWinEventHook(0x8000,0x800B,IntPtr.Zero,locationCallback,pid,0,0);
  stateHook=SetWinEventHook(3,0x17,IntPtr.Zero,stateCallback,0,0,0);
  if(locationHook==IntPtr.Zero||stateHook==IntPtr.Zero)throw new System.ComponentModel.Win32Exception(Texts.T("无法监听窗口变化","Could not monitor window changes"));
 }
 static bool IsMainAppWindow(IntPtr window){return AppWindows.IsMain(window);}
 static IntPtr FindMainAppWindow(){return AppWindows.Find();}
 static bool ForegroundMatches(IntPtr foreground,IntPtr target,IntPtr widget){uint a,b;GetWindowThreadProcessId(foreground,out a);GetWindowThreadProcessId(target,out b);return foreground==widget||(a!=0&&a==b);}
 async Task Refresh() {
  if(busy||closing||!AppWindows.IsExposed(target))return;busy=true;
  try {
   if(rpc==null) {rpc=new Rpc();await rpc.Start();}
   var data=await rpc.Call("account/rateLimits/read",new {});if(closing)return;
   Dictionary<string,object> limits=null;
   if(data.ContainsKey("rateLimitsByLimitId")&&data["rateLimitsByLimitId"] is Dictionary<string,object>) {var all=(Dictionary<string,object>)data["rateLimitsByLimitId"];if(all.ContainsKey("codex"))limits=all["codex"] as Dictionary<string,object>;}
   if(limits==null&&data.ContainsKey("rateLimits"))limits=data["rateLimits"] as Dictionary<string,object>;
   if(limits==null)throw new Exception(Texts.T("当前账户没有可用的额度数据","No usage limits are available for this account"));
   string a=Update(shortRing,limits,"primary"),b=Update(weekRing,limits,"secondary");updated=DateTime.UtcNow;SetTip(Texts.T("剩余额度","Remaining usage")+"\n"+a+"\n"+b+"\n"+Texts.T("更新于 ","Updated at ")+DateTime.Now.ToString("HH:mm:ss")+Texts.T(" · 每10秒刷新\n点击刷新 · 右键移动或退出"," · refreshes every 10 seconds\nClick to refresh · right-click for options"));
   File.WriteAllText(Path.Combine(directory,"status.json"),new JavaScriptSerializer().Serialize(new {updatedUtc=updated.Value.ToString("o"),shortRemaining=shortRing.Remaining,weekRemaining=weekRing.Remaining,primaryLabel=shortRing.Label,secondaryLabel=weekRing.Label,state="ok"}));
   if(Environment.GetCommandLineArgs().Contains("--preview"))SavePreview();
  } catch(Exception ex) {if(rpc!=null)rpc.Dispose();rpc=null;shortRing.Stale=weekRing.Stale=true;WriteStaleStatus();SetTip(Texts.T("刷新失败：","Refresh failed: ")+ex.Message+"\n"+(updated.HasValue?Texts.T("上次更新 ","Last updated ")+updated.Value.ToLocalTime().ToString("HH:mm:ss"):Texts.T("正在等待真实数据","Waiting for usage data"))+Texts.T("\n将在10秒后重试；点击可立即重试","\nRetrying in 10 seconds; click to retry now"));try {File.WriteAllText(Path.Combine(directory,"last-error.txt"),DateTime.Now.ToString("s")+" "+ex.Message);}catch{}}
  finally {busy=false;shortRing.InvalidateVisual();weekRing.InvalidateVisual();}
 }
 void WriteStaleStatus(){try{File.WriteAllText(Path.Combine(directory,"status.json"),new JavaScriptSerializer().Serialize(new {updatedUtc=updated.HasValue?updated.Value.ToString("o"):null,shortRemaining=shortRing.Remaining,weekRemaining=weekRing.Remaining,state="stale"}));}catch{}}
 string Update(Ring ring,Dictionary<string,object> limits,string key) {
  var w=limits.ContainsKey(key)?limits[key] as Dictionary<string,object>:null;
  if(w==null) {ring.Remaining=null;ring.Stale=true;return ring.Label+Texts.T("：暂无数据",": no data");}
  int minutes=Convert.ToInt32(w["windowDurationMins"]);ring.Label=minutes==10080?Texts.T("周","wk"):minutes==1440?Texts.T("日","day"):minutes%60==0?(minutes/60)+"h":minutes+"m";
  ring.Remaining=Math.Max(0,Math.Min(100,100-Convert.ToDouble(w["usedPercent"])));ring.Stale=false;
  var reset=DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(w["resetsAt"])).ToLocalTime();
  return ring.Label+": "+ring.Remaining.Value.ToString("0")+Texts.T("% · 重置 ","% · resets ")+reset.ToString("MM-dd HH:mm");
 }
 void SetTip(string text){((Border)Content).ToolTip=text;}
 void SavePreview(){Dispatcher.BeginInvoke(new Action(()=>{try {ContentPresenter view=new ContentPresenter {Content=ClonePreview()};view.Measure(new Size(162,366));view.Arrange(new Rect(0,0,162,366));var bmp=new RenderTargetBitmap(162,366,288,288,PixelFormats.Pbgra32);var border=ClonePreview();border.Measure(new Size(54,122));border.Arrange(new Rect(0,0,54,122));bmp.Render(border);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bmp));using(var f=File.Create(Path.Combine(directory,"preview.png")))encoder.Save(f);}catch{}}),DispatcherPriority.Render);}
 Border ClonePreview(){var s=new StackPanel {Margin=new Thickness(1,3,1,3)};s.Children.Add(new Ring {Width=50,Height=58,Label=shortRing.Label,Remaining=shortRing.Remaining,Stale=shortRing.Stale});s.Children.Add(new Ring {Width=50,Height=58,Label=weekRing.Label,Remaining=weekRing.Remaining,Stale=weekRing.Stale});return new Border {Width=54,Height=122,CornerRadius=new CornerRadius(16),Background=new SolidColorBrush(Color.FromRgb(250,249,253)),Child=s};}
}
}
