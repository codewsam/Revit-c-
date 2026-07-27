# Automatic dimensions for floor plan in Revit
# This script creates automatic dimensions for walls in the active view

import clr
clr.AddReference('RevitAPI')
clr.AddReference('RevitAPIUI')
clr.AddReference('System')

from Autodesk.Revit.DB import *
from Autodesk.Revit.UI import *
from System.Collections.Generic import List

doc = __revit__.ActiveUIDocument.Document
uidoc = __revit__.ActiveUIDocument
view = doc.ActiveView

def get_walls_in_view(view):
    """Get all walls visible in the current view"""
    collector = FilteredElementCollector(doc, view.Id)
    walls = collector.OfCategory(BuiltInCategory.OST_Walls)\
                     .WhereElementIsNotElementType()\
                     .ToElements()
    return walls

def get_dimension_type():
    """Get the first available linear dimension type"""
    collector = FilteredElementCollector(doc)
    dim_types = collector.OfClass(DimensionType).ToElements()
    for dt in dim_types:
        if dt.StyleType == DimensionStyleType.Linear:
            return dt
    return None

def create_wall_dimensions(wall, view, dim_type):
    """Create dimensions for a wall"""
    try:
        location = wall.Location
        if not isinstance(location, LocationCurve):
            return None
        
        curve = location.Curve
        if not isinstance(curve, Line):
            return None
        
        start_pt = curve.GetEndPoint(0)
        end_pt = curve.GetEndPoint(1)
        
        wall_dir = (end_pt - start_pt).Normalize()
        perp = XYZ(-wall_dir.Y, wall_dir.X, 0)
        
        # Offset for dimension line
        offset = 3.0  # feet
        
        mid_pt = (start_pt + end_pt) / 2
        dim_line_pt = mid_pt + perp * offset
        
        dim_line = Line.CreateBound(
            start_pt + perp * offset,
            end_pt + perp * offset
        )
        
        # Get references for the wall ends
        ref_array = ReferenceArray()
        
        # Get wall end references
        ref_start = wall.GetReferenceByName("START_REFERENCE_POINT") if hasattr(wall, 'GetReferenceByName') else None
        ref_end = wall.GetReferenceByName("END_REFERENCE_POINT") if hasattr(wall, 'GetReferenceByName') else None
        
        # Use geometry references instead
        opt = Options()
        opt.View = view
        opt.ComputeReferences = True
        opt.IncludeNonVisibleObjects = False
        
        geom_elem = wall.get_Geometry(opt)
        
        refs = []
        for geom_obj in geom_elem:
            if isinstance(geom_obj, Solid):
                for face in geom_obj.Faces:
                    normal = face.FaceNormal
                    # Get faces parallel to wall direction
                    if abs(normal.DotProduct(perp)) > 0.9:
                        refs.append(face.Reference)
        
        if len(refs) >= 2:
            ref_array.Append(refs[0])
            ref_array.Append(refs[1])
            
            dim = doc.Create.NewDimension(view, dim_line, ref_array, dim_type)
            return dim
        
        return None
        
    except Exception as e:
        print("Error creating dimension for wall {}: {}".format(wall.Id, str(e)))
        return None

def create_overall_dimensions(walls, view, dim_type):
    """Create overall dimensions grouping walls by direction"""
    
    horizontal_walls = []
    vertical_walls = []
    
    for wall in walls:
        location = wall.Location
        if not isinstance(location, LocationCurve):
            continue
        curve = location.Curve
        if
