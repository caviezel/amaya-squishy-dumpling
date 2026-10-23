using NUnit.Framework;
using UnityEngine;
using SquishyDumpling.Layout;

public class CameraFitTests
{
    [TestCase(900,1600,24f)] [TestCase(900,1950,29.25f)]
    [TestCase(900,2000,30f)] [TestCase(1200,1600,24f)]
    public void PortraitFitContainsCriticalRegion(int width,int height,float expected)
    {
        var viewport = new Rect(0,0,width,height);
        Assert.IsTrue(CameraFit.TryCalculate(viewport,viewport,out var fit));
        Assert.That(fit.HalfHeight,Is.EqualTo(expected).Within(.0001f));
        Assert.That(fit.Center,Is.EqualTo(Vector2.zero));
        AssertContainment(viewport,fit);
    }
    [TestCase(900,1950,0,80,900,1710)]
    [TestCase(900,2000,40,100,820,1760)]
    [TestCase(1200,1600,0,35,1140,1465)]
    [TestCase(1170,2532,0,102,1170,2289)] // Installed Device Simulator iPhone 13 Pro portrait
    [TestCase(1125,2436,0,102,1125,2202)] // Installed Device Simulator iPhone X portrait
    public void InsetsContainAllCorners(int w,int h,int x,int y,int sw,int sh)
    {
        var v=new Rect(0,0,w,h);
        Assert.IsTrue(CameraFit.TryCalculate(v,new Rect(x,y,sw,sh),out var fit));
        AssertContainment(v,fit);
    }
    [Test] public void AsymmetricCenterIsCompensated()
    {
        Assert.IsTrue(CameraFit.TryCalculate(new Rect(100,200,900,2000),new Rect(100,300,900,1700),out var fit));
        Assert.That(fit.Center.x,Is.EqualTo(0).Within(.001f));
        Assert.That(fit.Center.y,Is.EqualTo(1.5f).Within(.001f));
        AssertContainment(new Rect(100,200,900,2000),fit);
    }
    [Test] public void SafeAreaIsIntersectedWithInsetViewport()
    {
        var v=new Rect(100,200,700,1400);
        Assert.IsTrue(CameraFit.TryCalculate(v,new Rect(0,50,900,1800),out var fit));
        Assert.That(fit.UsablePixels,Is.EqualTo(v)); AssertContainment(v,fit);
    }
    [Test] public void InvalidOrDisjointViewportIsRejected()
    {
        Assert.IsFalse(CameraFit.TryCalculate(new Rect(0,0,0,100),new Rect(0,0,100,100),out _));
        Assert.IsFalse(CameraFit.TryCalculate(new Rect(0,0,100,100),new Rect(200,0,100,100),out _));
        Assert.IsFalse(CameraFit.TryCalculate(new Rect(0,0,float.NaN,100),new Rect(0,0,100,100),out _));
    }
    [Test] public void TallFitExceedsFiniteArtworkWithoutChangingRegistration()
    {
        CameraFit.TryCalculate(new Rect(0,0,900,2000),new Rect(0,0,900,2000),out var f);
        Assert.Greater(f.HalfHeight*2,CameraFit.ArtworkSize.y);
        Assert.AreEqual(new Vector2(40.96f,54.64f),CameraFit.ArtworkSize);
    }
    static void AssertContainment(Rect viewport,CameraFit.Result fit)
    {
        float a=viewport.width/viewport.height;
        foreach(float x in new[]{-13.5f,13.5f}) foreach(float y in new[]{-24f,24f})
        {
            float px=viewport.x+((x-fit.Center.x)/(2*fit.HalfHeight*a)+.5f)*viewport.width;
            float py=viewport.y+((y-fit.Center.y)/(2*fit.HalfHeight)+.5f)*viewport.height;
            Assert.That(px,Is.InRange(fit.UsablePixels.xMin-.01f,fit.UsablePixels.xMax+.01f));
            Assert.That(py,Is.InRange(fit.UsablePixels.yMin-.01f,fit.UsablePixels.yMax+.01f));
        }
    }
}
