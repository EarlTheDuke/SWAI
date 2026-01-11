using Microsoft.Extensions.Logging;
using SWAI.Core.Configuration;
using SWAI.Core.Interfaces;
using SWAI.Core.Models.Geometry;
using SWAI.Core.Models.Sketch;
using SWAI.Core.Models.Units;

namespace SWAI.SolidWorks.Services;

/// <summary>
/// Service for sketch operations
/// </summary>
public class SketchService : ISketchService
{
    private readonly ILogger<SketchService> _logger;
    private readonly SolidWorksService _swService;
    private readonly SolidWorksConfiguration _config;
    private SketchProfile? _currentSketch;

    public SketchService(
        SolidWorksService swService,
        SolidWorksConfiguration config,
        ILogger<SketchService> logger)
    {
        _swService = swService;
        _config = config;
        _logger = logger;
    }

    public async Task<SketchProfile> CreateSketchAsync(string name, ReferencePlane plane)
    {
        _logger.LogInformation("Creating sketch '{Name}' on {Plane}", name, plane);

        var sketch = new SketchProfile(name, plane);

        if (!_config.UseMock)
        {
            await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null)
                    throw new InvalidOperationException("Not connected to SolidWorks");

                var model = swApp.ActiveDoc;
                if (model == null)
                    throw new InvalidOperationException("No active document");

                // Select the reference plane
                var planeName = Plane.GetSolidWorksName(plane);
                model.Extension.SelectByID2(planeName, "PLANE", 0, 0, 0, false, 0, null, 0);

                // Insert sketch
                model.SketchManager.InsertSketch(true);
                _logger.LogInformation("Sketch created on {Plane}", plane);
            });
        }

        _currentSketch = sketch;
        return sketch;
    }

    public async Task<bool> EditSketchAsync(SketchProfile sketch)
    {
        _logger.LogInformation("Editing sketch: {Name}", sketch.Name);
        _currentSketch = sketch;

        if (!_config.UseMock)
        {
            await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                var model = swApp?.ActiveDoc;
                if (model == null) return;

                // Select and edit the sketch
                model.Extension.SelectByID2(sketch.Name, "SKETCH", 0, 0, 0, false, 0, null, 0);
                model.SketchManager.InsertSketch(true);
            });
        }

        return true;
    }

    public async Task<bool> CloseSketchAsync()
    {
        _logger.LogInformation("Closing sketch");

        if (!_config.UseMock)
        {
            await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                var model = swApp?.ActiveDoc;
                if (model == null) return;

                model.SketchManager.InsertSketch(true); // Toggles sketch mode off
            });
        }

        _currentSketch = null;
        return true;
    }

    public async Task<SketchRectangle> AddRectangleAsync(Point3D corner1, Point3D corner2)
    {
        var rect = new SketchRectangle(corner1, corner2);
        _logger.LogInformation("Adding rectangle: {Rect}", rect);

        if (!_config.UseMock)
        {
            await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                var model = swApp?.ActiveDoc;
                if (model == null) return;

                var (x1, y1, z1) = corner1.ToMeters();
                var (x2, y2, z2) = corner2.ToMeters();

                // Create corner rectangle
                model.SketchManager.CreateCornerRectangle(x1, y1, z1, x2, y2, z2);
                _logger.LogInformation("Rectangle created in SolidWorks");
            });
        }

        _currentSketch?.AddRectangle(rect);
        return rect;
    }

    public async Task<SketchRectangle> AddCenteredRectangleAsync(Point3D center, Dimension width, Dimension height)
    {
        var halfWidth = width / 2;
        var halfHeight = height / 2;

        var corner1 = new Point3D(
            center.X - halfWidth,
            center.Y - halfHeight,
            center.Z
        );
        var corner2 = new Point3D(
            center.X + halfWidth,
            center.Y + halfHeight,
            center.Z
        );

        var rect = new SketchRectangle(corner1, corner2);
        _logger.LogInformation("Adding centered rectangle: {Width} x {Height}", width, height);

        if (!_config.UseMock)
        {
            await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                var model = swApp?.ActiveDoc;
                if (model == null) return;

                var (x1, y1, z1) = corner1.ToMeters();
                var (x2, y2, z2) = corner2.ToMeters();

                // Create center rectangle
                model.SketchManager.CreateCenterRectangle(
                    center.X.Meters, center.Y.Meters, center.Z.Meters,
                    x2, y2, z2
                );
                _logger.LogInformation("Centered rectangle created in SolidWorks");
            });
        }

        _currentSketch?.AddRectangle(rect);
        return rect;
    }

    public async Task<SketchCircle> AddCircleAsync(Point3D center, Dimension radius)
    {
        var circle = new SketchCircle(center, radius);
        _logger.LogInformation("Adding circle: R={Radius} at {Center}", radius, center);

        if (!_config.UseMock)
        {
            await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                var model = swApp?.ActiveDoc;
                if (model == null) return;

                var (cx, cy, cz) = center.ToMeters();

                // Create circle by center and radius
                model.SketchManager.CreateCircleByRadius(cx, cy, cz, radius.Meters);
                _logger.LogInformation("Circle created in SolidWorks");
            });
        }

        _currentSketch?.AddCircle(circle);
        return circle;
    }

    public async Task<SketchLine> AddLineAsync(Point3D start, Point3D end)
    {
        var line = new SketchLine(start, end);
        _logger.LogInformation("Adding line: {Start} to {End}", start, end);

        if (!_config.UseMock)
        {
            await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                var model = swApp?.ActiveDoc;
                if (model == null) return;

                var (x1, y1, z1) = start.ToMeters();
                var (x2, y2, z2) = end.ToMeters();

                model.SketchManager.CreateLine(x1, y1, z1, x2, y2, z2);
                _logger.LogInformation("Line created in SolidWorks");
            });
        }

        _currentSketch?.AddLine(line);
        return line;
    }
}
