# MozJPEG-GUI

用 C# 和 WPF 折腾的一个使用 [MozJPEG](https://github.com/mozilla/mozjpeg) 进行压图的 GUI 程序。

运行前请确定已经在电脑上安装了 .NET Framework 4.0。

### 特性

* 单文件绿色版
* 支持对 PNG 格式图片进行转换（原版 MozJPEG 不支持）
* 在转换时对图片进行缩放
* 批处理功能

### 使用截图

![](https://ae01.alicdn.com/kf/HTB13k_NTYvpK1RjSZFq763XUVXap.png)

>本程序使用的 MozJPEG 预编译版程序 cjpeg.exe 来自于 [garyzyg/mozjpeg-windows](https://github.com/garyzyg/mozjpeg-windows/releases)。  
>为减小主程序大小，对 cjpeg.exe 使用了 `upx --ultra-brute` 进行压缩。