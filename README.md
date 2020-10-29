[![.NET Core](https://github.com/imazen/imageflow-dotnet-server/workflows/.NET%20Core/badge.svg)](https://github.com/imazen/imageflow-dotnet-server/actions?query=workflow%3A%22.NET+Core%22) [![Build status](https://ci.appveyor.com/api/projects/status/5hm0ekhe455i56fp/branch/main?svg=true)](https://ci.appveyor.com/project/imazen/imageflow-dotnet-server/branch/main)

#### Imageflow.NET Server is image processing and optimizing middleware for ASP.NET Core 3.1+. 

If you don't need an HTTP server, [try Imageflow.NET](https://github.com/imazen/imageflow-dotnet). If you don't want to use .NET, try [Imageflow](https://imageflow.io), which has a server, command-line tool, and library with language bindings for Go, C, Rust, Node, Ruby and more. Imageflow is specifically designed for web servers and focuses on security, quality, and performance. 

**Serving optimized and correctly sized images is the fastest way to a quicker, more profitable site or app. 60% of website bytes are from images<sup>[1]</sup>.**

Imageflow.NET Server edits and optimizes images so quickly you can do it on-demand. No need to manually generate every size/format combination of every image.

![Imageflow Server Diagram](https://www.imageflow.io/images/imageflow-responsive.svg)![Querystring animation](https://www.imageflow.io/images/edit-url.gif)


<sup>[1]</sup>According to the HTTP Archive, 60% of the data transferred to fetch a web page is images composed of JPEGs, PNGs and GIFs.

### What else can it do?

* Automatically crop away whitespace
* Sharpen
* Fix white balance
* Apply watermarks
* Adjust contrast/saturation/brightness
* Rotate & Flip images
* Crop
* Resize & Constrain
* Produce highly optimized jpeg images to reduce download times
* [More](https://docs.imageflow.io)

All operations are designed to be fast enough for on-demand use.

### Features

* Supports Windows, Mac, and Linux
* Comes with [Dockerfiles](https://github.com/imazen/imageflow-dotnet-server/tree/main/examples/Imageflow.Server.ExampleDockerDiskCache) for cloud deployment.
* Processes images located on disk, Azure Blob Storage or Amazon S3
* Disk Caching
* Memory Caching
* Distributed Caching
* Mapping arbitrary virtual paths to physical ones. 
* Imageflow's [Querystring API](https://docs.imageflow.io/querystring/introduction.html) (compatible with ImageResizer)
* Production-ready for trusted image files. 


### License

We offer commercial licenses at https://imageresizing.net/pricing, or you can use
Imageflow, Imageflow.NET and Imageflow.NET Server under the terms of the AGPLv3. License keys are not yet required for commercial use, but we ask that you buy a license to help fund development of the Imageflow product suite. 

### For users coming from ImageResizer

For help migrating from ImageResizer, see [the migrating from ImageResizer](#migrating-from-imageresizer) section and open an issue or email `support@imazen.io` if you have any questions. 

## Basic Installation

You can look at `examples/Imageflow.Server.ExampleMinimal` to see the result. 

These steps assume you want to serve and modify images from the `wwwroot` folder. 
You can call `.SetMapWebRoot(false).MapPath("/", physicalPath)` to map a different physical folder. 
For examples on serving files from S3 or Azure, see the full example after this. 

1. Create a new ASP.NET Core 3.1 project using the Empty template. 
2. Create a directory called "wwwroot" and add a file "image.jpg"
3. Install Imageflow.Server
    ```
    Install-Package Imageflow.Server
    ```
4. Open Startup.cs and edit the Configure method.  Add
    ```c#
    app.UseImageflow(new ImageflowMiddlewareOptions()
        .SetMapWebRoot(true));
    ```
5. Replace the endpoint with something that generates an image tag, like 
   ```c#
   app.UseEndpoints(endpoints =>
   {
       endpoints.MapGet("/", async context =>
       {
           context.Response.ContentType = "text/html";
           await context.Response.WriteAsync("<img src=\"image.jpg?width=450\" />");
       });
   });
   ```
6. Run your project and see the image be dynamically resized. 

## Installing everything

See `examples/Imageflow.Server.Example` for this example. 

```
Install-Package Imageflow.Server
Install-Package Imageflow.Server.DiskCache
Install-Package Imageflow.Server.Storage.S3
Install-Package Imageflow.Server.Storage.AzureBlob
```

Note: Older versions of Windows may not have the C Runtime 
installed ([Install 32-bit](https://aka.ms/vs/16/release/vc_redist.x86.exe) or [64-bit](https://aka.ms/vs/16/release/vc_redist.x64.exe)). 


```c#
using System.IO;
using Amazon;
using Azure.Storage.Blobs;
using Imageflow.Fluent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Imageflow.Server.DiskCache;
using Imageflow.Server.Storage.AzureBlob;
using Imageflow.Server.Storage.S3;

namespace Imageflow.Server.Example
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        private IConfiguration Configuration { get; }
        private IWebHostEnvironment Env { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            
            // Make S3 containers available at /ri/ and /imageflow-resources/
            // If you use credentials, do not check them into your repository
            services.AddImageflowS3Service(new S3ServiceOptions(null, null)
                .MapPrefix("/ri/", RegionEndpoint.USEast1, "resizer-images")
                .MapPrefix("/imageflow-resources/", RegionEndpoint.USWest2, "imageflow-resources"));

            // Make Azure container available at /azure
            services.AddImageflowAzureBlobService(
                new AzureBlobServiceOptions(
                        "UseDevelopmentStorage=true;",
                        new BlobClientOptions())
                    .MapPrefix("/azure", "imageflow-demo" ));

            // You can add a distributed cache, such as redis, if you add it and and
            // call ImageflowMiddlewareOptions.SetAllowDistributedCaching(true)
            services.AddDistributedMemoryCache();
            // You can add a memory cache and call ImageflowMiddlewareOptions.SetAllowMemoryCaching(true)
            services.AddMemoryCache();
            // You can add a disk cache and call ImageflowMiddlewareOptions.SetAllowDiskCaching(true)
            // If you're deploying to azure, provide a disk cache folder *not* inside ContentRootPath
            // to prevent the app from recycling whenever folders are created.
            services.AddImageflowDiskCache(new DiskCacheOptions(Path.Combine(Env.ContentRootPath, "imageflow_cache")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();

            // You have a lot of configuration options
            app.UseImageflow(new ImageflowMiddlewareOptions()
                // Maps / to WebRootPath
                .SetMapWebRoot(true) 
                // Maps /folder to ContentRootPath/folder
                .MapPath("/folder", Path.Combine(Env.ContentRootPath, "folder"))
                // Allow Disk Caching
                .SetAllowDiskCaching(true)
                // We can only have one type of caching enabled at a time
                .SetAllowDistributedCaching(false)
                // Disable memory caching even if the service is installed
                .SetAllowMemoryCaching(false)
                // Cache publicly (including on shared proxies and CDNs) for 30 days
                .SetDefaultCacheControlString("public, max-age=2592000")
                // Allows extensionless images to be served within the given directory(ies)
                .HandleExtensionlessRequestsUnder("/customblobs/", StringComparison.OrdinalIgnoreCase)                
                // Force all paths under "/gallery" to be watermarked
                .AddRewriteHandler("/gallery", args =>
                {
                    args.Query["watermark"] = "imazen";
                })
                .AddCommandDefault("down.filter", "mitchell")
                .AddCommandDefault("f.sharpen", "15")
                .AddCommandDefault("ignore_icc_errors", "true")
                //When set to true, this only allows ?preset=value URLs, returning 403 if you try to use any other commands. 
                .SetUsePresetsExclusively(false)
                .AddPreset(new PresetOptions("large", PresetPriority.DefaultValues)
                    .SetCommand("width", "1024")
                    .SetCommand("height", "1024")
                    .SetCommand("mode", "max"))
                //When set to true, this only allows urls with a &signature, returning 403 if missing/invalid. 
                //Use Imazen.Common.Helpers.Signatures.SignRequest(string pathAndQuery, string signingKey) to generate
                .SetRequireRequestSignature(false)
                .AddRequestSigningKey("test key")
                // It's a good idea to limit image sizes for security. Requests causing these to be exceeded will fail
                // The last argument to FrameSizeLimit() is the maximum number of megapixels
                .SetJobSecurityOptions(new SecurityOptions()
                    .SetMaxDecodeSize(new FrameSizeLimit(8000,8000, 40))
                    .SetMaxFrameSize(new FrameSizeLimit(8000,8000, 40))
                    .SetMaxEncodeSize(new FrameSizeLimit(8000,8000, 20)))
                // Register a named watermark that floats 10% from the bottom-right corner of the image
                // With 70% opacity and some sharpness applied. 
                .AddWatermark(
                    new NamedWatermark("imazen",
                        "/images/imazen_400.png",
                        new WatermarkOptions()
                            .SetFitBoxLayout(
                                new WatermarkFitBox(WatermarkAlign.Image, 10, 10, 90, 90),
                                WatermarkConstraintMode.Within,
                                new ConstraintGravity(100, 100))
                            .SetOpacity(0.7f)
                            .SetHints(
                                new ResampleHints()
                                    .SetResampleFilters(InterpolationFilter.Robidoux_Sharp, null)
                                    .SetSharpen(7, SharpenWhen.Downscaling))
                            .SetMinCanvasSize(200, 150)))
                .AddWatermarkingHandler("/", args =>
                {
                    if (args.Query.TryGetValue("water", out var value) && value == "mark")
                    {
                        args.AppliedWatermarks.Add(new NamedWatermark(null, "/images/imazen_400.png", new WatermarkOptions()));
                    }
                }));
            
            
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}

```

## Migrating from ImageResizer

### General Notes
* Imageflow does not let shadows overwhelm image highlights 
  (it resizes images in linear RGB instead of averaging compressed sRGB 
  values). This is correct behavior, but can lower the visual impact of 
  certain images while improving most. To restore the old behavior add `down.colorspace=srgb`.
  To do this site-wide, use `.AddCommandDefault("down.colorspace", "srgb")`
* Imageflow focuses on correctness, so it does not over-sharpen images by default. For some image types, 
  additional sharpening is appropriate. You can make images slightly sharper by using
  the Mitchell resampling filter with `.AddCommandDefault("down.filter", "mitchell")`. 
  You can add stronger sharpening with `.AddCommandDefault("f.sharpen", "15")`
* Unlike ImageResizer, Imageflow does not support .TIFF files. Please convert them to 
.png or .jpg before migrating to Imageflow.NET Server. There is no secure open-source codec for .TIFF files, so we chose to not support the format.
* Nearly all querystring commands are supported, with few infrequently exceptions:
    * We no longer support adding borders to images as that can be done better in CSS.
    * We no longer support rounding the corners of images or adding drop shadows; this can also be done in CSS.
    * Rotation must be in intervals of 90 degrees.
    * Blurring and noise removal is not yet supported.
* Blob storage providers now expect blobs to be treated as immutable, as there is too much latency to check the modified date.
* Most popular plugins are now built-in, including WebP, AnimatedGifs,
    PrettyGifs, SimpleFilters, FastScaling, Watermark, VirtualFolder,
     ClientCache, AutoRotate, and WhitespaceTrimmer.
* The following plugins are not available: DropShadow,
    Gradient, Image404, RedEye, Faces, SeamCarving, WIC, TinyCache, 
    PsdReader, PsdComposer, MongoReader, FreeImage, FFMpeg, 
    AdvancedFilters, CopyMetadata. 
* SqlReader functionality can be replicated by implementing Imazen.Common.Storage.IBlobProvider.
* Blob Providers now only need to implement 
    Imazen.Common.Storage.IBlobProvider, which greatly simplifies plugging in new storage.


 ### Querystring Command Migration Details
 
 Nearly all features are supported
 
* The following commands are supported: `mode`, `anchor`, `flip`, `sflip`,
    `quality`, `zoom`, `dpr`, `crop`, `cropxunits`, `cropyunits`,
    `w`, `h`, `width`, `height`, `maxwidth`, `maxheight`, `format`,
    `srotate`, `rotate`, `stretch`, `webp.lossless`, `webp.quality`,
    `f.sharpen`, `f.sharpen_when`, `down.colorspace`, `bgcolor`, 
    `jpeg_idct_downscale_linear`, `watermark`, `s.invert`, `s.sepia`, 
    `s.grayscale`, `s.alpha`, `s.brightness`, `s.contrast`, `s.saturation`, 
    `trim.threshold`, `trim.percentpadding`, `a.balancewhite`,  `jpeg.progressive`,
    `decoder.min_precise_scaling_ratio`, `scale`, `preset`
 * TIFF files are not supported, so `page=x` is not supported.
 * Animated GIFs are fully supported, so `frame=x` is ignored.
 * Images are always auto-rotated based on Exif information, so `autorotate` is ignored.
 * Images can only be rotated in 90 degree intervals, so `rotate` is partially supported.
 * PNG encoding adapts the palette size as needed, so `colors` is ignored.
 * PNG and GIFs are always dithered, so `dither` is ignored.
 * Jpeg subsampling is auto-selected by chroma evaluation, so `subsampling` is ignored.
 * Adding margins, padding, and borders to images is obsolete, so 
    `paddingwidth`, `paddingheight`, `margin`
    `borderwidth`, `bordercolor` and `paddingcolor` are now ignored. 
 * Rounding corners is not supported, so `s.roundcorners` is ignored.
 * Caching, processing, and encoders/builders/decoders are not configurable via the querystring,
    so `cache`, `process`, `encoder`, `decoder`, and `builder` are ignored.
 * Sharpening is now done with `f.sharpen`, not `a.sharpen`, and `a.sharpen` is ignored.
 * Noise removal is not yet supported, so `a.removenoise` is ignored.
 * Blurring is not yet supported, so `a.blur` is ignored.
 * ICC profiles are never ignored, so `ignoreicc` is ignored.
 * 404 redirects are not implemented, so `404` is ignored.
