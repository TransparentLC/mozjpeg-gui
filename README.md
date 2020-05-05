# MozJPEG-GUI

用 C# 和 WPF 折腾的一个使用 [MozJPEG](https://github.com/mozilla/mozjpeg) 以 JPEG 格式压缩图片的 GUI 程序。

**现在也支持调用 [libwebp](https://developers.google.com/speed/webp/docs/cwebp)，将图片压缩为 WebP 格式了！** 不过懒得改名字了……

运行前请确定已经在电脑上安装了 .NET Framework 4.0。

### 使用截图

![](https://ae01.alicdn.com/kf/H7f9532591a78404b965f2b02a28b1529a.png)

### 特性

* 只有单个文件，是**绿色版**程序。
    * `cjpeg.exe` 使用的是来自于 [garyzyg/mozjpeg-windows](https://github.com/garyzyg/mozjpeg-windows/releases) 的预编译版（3.3.1 x64）， `cwebp.exe` 和 `dwebp.exe` 使用的是来自于 [Google 官方](https://storage.googleapis.com/downloads.webmproject.org/releases/webp/index.html)的预编译版（1.0.3 x64）。
    * 以上程序的二进制文件使用了 [UPX](https://github.com/upx/upx) 及参数 `--ultra-brute` 进行了压缩，作为资源文件嵌入到程序本体中，程序启动时会将它们释放到临时目录以在运行时调用，在退出时删除。
* 支持**打开 PNG 和 WebP 格式**的图片。
    * MozJPEG 只支持输入 TGA、BMP、PPM、JPEG 四种格式的图片，**不支持 PNG**（当然更不可能支持 WebP），程序会自动将打开的图片转换为 BMP，存储在临时目录。
    * JPEG 不支持透明度，所以 PNG 图片的透明部分会变成白色。
    * 暂不支持 GIF 转换为 Animated WebP，打开 GIF 图片时只会转换第一帧。
* 参考了 Photoshop 的“存储为 Web 所用格式”界面，加入了**预览编码后的图片**和**保存图片时对图片进行缩放**的功能。
    * 预览功能实际上是按照参数对图片进行编码后将输出的图片显示在窗口中，如果要保存的话将预览的图片复制粘贴即可。
    * 缩放功能是先对原图进行缩放，存储在临时目录，然后将缩放后的图片拿去编码，MozJPEG 本身没有缩放图片的功能。
* **批处理功能**，可以对一个文件夹的所有图片进行编码。编码设定使用的是窗口上的设定，还可以选择按宽度/高度/百分比对批处理中的图片进行缩放。
    * “批处理”选择“按百分比缩放”，将百分比设为 50%，缩放后的图片宽度和高度都是原图的一半，设为 100% 就是不进行缩放。
    * “批处理”选择“等比例缩放至宽度”，将宽度设为 1000，缩放后的图片将按纵横比缩放至宽度为 1000 的大小。例如一张 6000x4000 的图片（也就是 3:2 的比例）就会被缩放到 1000x667。“等比例缩放至高度”同理。
* **JPEG 图片格式的设定**，可以根据需要编码不同类型的图片（不改变质量，会改变文件大小）。不同格式的图片在网页上加载时显示的加载过程也有所不同。
    * “基本式”（baseline），图片从上至下逐渐显示。
    * “渐进式”（progressive），图片从模糊逐渐变清晰。
    * “渐进式”又可以继续分为“交错式”（interleaved）和“非交错式”（non-interleaved）。在加载模糊部分时，前者同时加载所有色彩通道；后者先加载亮度通道再加载色度通道，表现为先显示黑白图片，再逐渐向图片上添加颜色。一般情况下“非交错式”的图片文件大小要略微小于“交错式”。
    * 使用了 Firefox 的开发者工具[模拟在低速网络环境下打开图片的视频](https://files.catbox.moe/8derzy.mp4)，展示了不同格式的图片在加载过程中的具体效果。
* WebP 图片格式的设定，包括是否使用无损编码、设定滤波强度等等，参见 [Google 官方说明](https://developers.google.com/speed/webp/docs/cwebp)。
* 图片**一键上传**：将压缩后的图片上传到 ~~`ae01.alicdn.com`~~（换了另外一个图床）。
    * [非公开图床](https://blog.cmcncm.cn/2019/03/26/image-hosting/#%E9%98%BF%E9%87%8C%E5%B7%B4%E5%B7%B4)之一，速度不错，而且支持上传 WebP 格式的图片（但是需要将扩展名修改为 `.jpg` 进行“伪装”……），点击[这里](https://yzf.qq.com/fsnb/kf-file/kf_pic/20200505/KFPIC_dC_WXIMAGE_kBrALQhmvbGDICWIenpW.jpg)查看示例。