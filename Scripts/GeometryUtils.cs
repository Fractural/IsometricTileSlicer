using Godot;
using System.Collections.Generic;
using System.Linq;

public static class StringUtils
{
    public static string TryGetSiblingOrChildRelativeFilePath(this string path, string sibilingOrChildPath)
    {
        var baseDir = path.GetBaseDir();
        if (path == "" || !sibilingOrChildPath.StartsWith(baseDir))
            return sibilingOrChildPath;
        return "." + sibilingOrChildPath.Substring(baseDir.Length);
    }

    public static string TryGetAbsolutePath(this string path, string relativePath)
    {
        if (relativePath.StartsWith("./"))
            return path.GetBaseDir() + relativePath.Substring(1);
        return relativePath;
    }
}

public static class GeometryUtils
{
    public static Vector2[][] MergePolygons(params Vector2[][] polygons)
    {
        bool mergedOnce = false;
        List<Vector2[]> polygonsList = polygons.ToList();
        do
        {
            mergedOnce = false;

            HashSet<Vector2[]> checkedPolygons = new HashSet<Vector2[]>();
            List<Vector2[]> newMergedPolygons = new List<Vector2[]>();
            foreach (var polygon in polygonsList)
            {
                if (checkedPolygons.Contains(polygon))
                    continue;
                checkedPolygons.Add(polygon);

                Vector2[] currentMergePolygon = polygon;

                foreach (var otherPolygon in polygonsList)
                {
                    if (checkedPolygons.Contains(otherPolygon))
                        continue;

                    var result = Geometry.MergePolygons2d(currentMergePolygon, otherPolygon);
                    if (Geometry.IntersectPolygons2d(currentMergePolygon, otherPolygon).Count > 0)
                    {
                        // If we've merged successfully, then consider the polygon
                        // that we've merged into as checked
                        checkedPolygons.Add(otherPolygon);
                        mergedOnce = true;
                        currentMergePolygon = result[0] as Vector2[];
                    }
                }

                newMergedPolygons.Add(currentMergePolygon);
            }
            if (mergedOnce)
            {
                polygonsList = newMergedPolygons;
            }
            // If we've merged once this iteration, then we may be able to merge again
        } while (mergedOnce);

        return polygonsList.ToArray();
    }
}