using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace Manicotti
{
    [Transaction(TransactionMode.Manual)]
    public class Sandbox : IExternalCommand
    {
        // Perform curve splitting according to raw intersection parameters
        public List<Curve> crvSplit(Curve parent, List<double> parameters)
        {
            List<Curve> segments = new List<Curve>();
            parameters.Add(parent.GetEndParameter(1));
            parameters.Insert(0, parent.GetEndParameter(0));
            double[] params_ordered = parameters.ToArray();
            Array.Sort(params_ordered);
            //Debug.Print(params_ordered.Length.ToString());
            for (int index = 0; index < parameters.Count - 1; index++)
            {
                Curve segment = parent.Clone();
                segment.MakeBound(params_ordered[index], params_ordered[index + 1]);
                segments.Add(segment);
            }
            //Debug.Print("Add new shatters " + segments.Count().ToString());
            return segments;
        }


        // Shatter a bunch of curves according to their intersections 
        public List<Curve> crvExplode(List<Curve> C)
        {
            // Start by Shattering all of your input curves by intersecting them with each other
            List<Curve> shatters = new List<Curve>();
            for (int CStart = 0; CStart <= C.Count - 1; CStart++)
            {
                List<double> breakParams = new List<double>();
                for (int CCut = 0; CCut <= C.Count - 1; CCut++)
                {
                    if (CStart != CCut)
                    {
                        SetComparisonResult result = C[CStart].Intersect(C[CCut], out IntersectionResultArray results);
                        if (result != SetComparisonResult.Disjoint)
                        {
                            //XYZ breakPt = results.get_Item(0).XYZPoint;
                            //Debug.Print("The intersection point is (" + breakPt.X.ToString() + ", " + breakPt.Y.ToString() + ")");
                            //double breakParam = C[CStart].Project(breakPt).Parameter;
                            // Another way to get the intersection parameter
                            double breakParam = results.get_Item(0).UVPoint.U;
                            breakParams.Add(breakParam);
                            Debug.Print("Projection parameter is: " + breakParam.ToString());
                            //Debug.Print("The raw parameter of the No." + CStart.ToString() + " intersection is " + breakParam.ToString());
                        }
                    }
                }
                shatters.AddRange(crvSplit(C[CStart], breakParams));
            }
            return shatters;
        }


        // Clean stray edges and form space boundaries by intersection. WIP
        public List<Curve> Trim(List<Curve> C) // , Plane P, ref object CN
        {
            List<Curve> Crvs = new List<Curve>();
            for (int CStart = 0; CStart <= C.Count - 1; CStart++)
            {
                List<double> breakParams = new List<double>();
                for (int CCut = 0; CCut <= C.Count - 1; CCut++)
                {
                    if (CStart != CCut)
                    {
                        SetComparisonResult result = C[CStart].Intersect(C[CCut], out IntersectionResultArray results);
                        if (result != SetComparisonResult.Disjoint)
                        {
                            double breakParam = results.get_Item(0).UVPoint.U;
                            breakParams.Add(breakParam);
                            Debug.Print("Projection parameter is: " + breakParam.ToString());
                        }
                    }
                }
                Crvs.AddRange(crvSplit(C[CStart], breakParams));
            }

            // Set "half-curve" lists
            List<XYZ> Vtc = new List<XYZ>(); // all unique vertices
            List<Curve> HC = new List<Curve>(); // list of half-curves
            List<int> HCI = new List<int>(); // half curve indices
            List<int> HCO = new List<int>(); // half curve opposites
            List<int> HCN = new List<int>(); // next index for each half-curve
            List<int> HCV = new List<int>(); // half-curve vertex
            List<int> HCF = new List<int>(); // half-curve face
            List<Plane> HCPln = new List<Plane>(); // half-curve plane
            List<bool> HCK = new List<bool>(); // flag if a half-curve needs to be killed (if it either starts or ends hanging)
            Dictionary<int, List<Curve>> F = new Dictionary<int, List<Curve>>(); // data tree for faces
            Dictionary<int, List<int>> VOut = new Dictionary<int, List<int>>(); // data tree of outgoing half-curves from each vertex

            foreach (Curve Crv in Crvs) // cycle through each curve
            {
                for (int CRun = 0; CRun <= 2; CRun += 2) // create two half-curves: first in one direction, and then the other...
                {
                    XYZ testedPt = new XYZ();
                    if (CRun == 0) {
                        HC.Add(Crv);
                        testedPt = Crv.GetEndPoint(0);
                    } else {
                        HC.Add(Crv.CreateReversed());
                        testedPt = Crv.GetEndPoint(1);
                    }
                    HCI.Add(HCI.Count); // count this iteration
                    HCO.Add(HCI.Count - CRun); // a little index trick
                    HCN.Add(-1);
                    HCF.Add(-1);
                    HCK.Add(false);

                    int VtcSet = -1;

                    for (int VtxCheck = 0; VtxCheck <= Vtc.Count - 1; VtxCheck++)
                    {
                        if (Vtc[VtxCheck].DistanceTo(testedPt) < 0.0000001)
                        {
                            VtcSet = VtxCheck; // get the vertex index, if it already exists
                            break;
                        }
                    }

                    if (VtcSet > -1)
                    {
                        HCV.Add(VtcSet); // If the vertex already exists, set the half-curve vertex
                        VOut[VtcSet].Add(HCI.Last());
                    }
                    else
                    {
                        HCV.Add(Vtc.Count); // if the vertex doesn't already exist, add a new vertex index
                        VOut.Add(Vtc.Count, new List<int>() { HCI.Last() });
                        // add the new half-curve index to the list of outgoing half-curves associated with the vertex
                        Vtc.Add(testedPt);
                        // add the new vertex to the vertex list
                    }
                    /*
                    // create, align and add the plane to be used for sequencing half-curves
                    Plane AddPlane = new Plane(Vtc[HCV.Last()], P.ZAxis);
                    AddPlane.Rotate(Vector3d.VectorAngle(AddPlane.XAxis, Crv.TangentAtStart, AddPlane), AddPlane.ZAxis);
                    HCPln.Add(AddPlane);
                    */
                    Crv.CreateReversed(); // reverse the curve for creating the opposite half-curve in the second part of the loop
                    Debug.Print("Tested point is (" + testedPt.X.ToString() + ", " + testedPt.Y.ToString() + ")");
                }
            }

            Debug.Print("Elements inside Crvs are " + Crvs.Count.ToString());
            Debug.Print("Elements inside HC are " + HC.Count.ToString());
            Debug.Print("Elements inside VOut are " + VOut.Count.ToString());

            // For each Vertex that has only one outgoing half-curve, kill the half-curve and its opposite
            foreach (KeyValuePair<int, List<int>> path in VOut)
            {
                Debug.Print("This point has been connected to " + path.Value.Count.ToString() + " curves");
                if (path.Value.Count == 1)
                {
                    HCK[path.Value[0]] = true;
                    HCK[HCO[path.Value[0]]] = true;
                }
            }

            // Remove the stray edges
            for (int i = HC.Count - 1; i >= 0; i--)
            {
                if (HCK[i])
                {
                    HC.Remove(HC[i]);
                }
            }

            // Remove the duplicated (reversed) shatters
            for (int i = HC.Count - 1; i >= 0; i--)
            {
                if (i % 2 == 1)
                {
                    HC.Remove(HC[i]);
                }
            }
            Debug.Print("Elements inside HC (after the deletion) are " + HC.Count.ToString());

            /*
            // Find the "next" half-curve for each starting half curve by identifying the outgoing half-curve from the end vertex
            // that presents the smallest angle by calculating its plane's x-axis angle from x-axis of the starting half-curve's opposite plane
            foreach (int HCIdx in HCI)
            {
                Plane PlaneUse = HCPln[HCO[HCIdx]];
                int MinIdx = -1;
                double MinAngle = 2 * Math.PI;
                foreach (int HCOut in VOut.Branch(HCV[HCO[HCIdx]]))
                {
                    if (HCOut != HCO[HCIdx] & HCK[HCIdx] == false & HCK[HCOut] == false)
                    {
                        double AngleTest = Vector3d.VectorAngle(PlaneUse.XAxis, HCPln[HCOut].XAxis, PlaneUse);
                        if (AngleTest < MinAngle)
                        {
                            MinIdx = HCOut;
                            MinAngle = AngleTest;
                        }
                    }
                }
                HCN[HCIdx] = MinIdx;
            }
            */

            /*
            // Sequence half-curves into faces by running along "next" half-curves in order until the starting half-curve is returned to
            List<int> FaceEdges = new List<int>();
            List<int> DeleteEdges = new List<int>();

            // cycle through each half-curve
            foreach (int HCIdx in HCI)
            {
                int EmExit = 0;
                if (HCF[HCIdx] == -1)
                {
                    int EdgeCounter = 1;
                    int FaceIdx = F.Paths.Count;
                    int CurrentIdx = HCIdx;
                    F.Add(HC[CurrentIdx], new GH_Path(FaceIdx));
                    HCF[CurrentIdx] = FaceIdx;
                    do
                    {
                        if (HCN[CurrentIdx] == -1)
                        {
                            DeleteEdges.Add(FaceIdx);
                            break;
                        }

                        CurrentIdx = HCN[CurrentIdx];
                        F.Add(HC[CurrentIdx], new GH_Path(FaceIdx));
                        EdgeCounter += 1;
                        HCF[CurrentIdx] = FaceIdx;
                        if (HCN[CurrentIdx] == HCIdx)
                            break;
                        EmExit += 1;
                        if (EmExit == Crvs.Count - 1)
                            break;
                    }
                    while (true)// and will be added to the delete list// exit once the starting half-curve is reached again // emergency exit prevents infinite loops
            ;
                    FaceEdges.Add(EdgeCounter);
                }
            }
            */

            /*
            // Find the perimeter by counting edges...it's possible that an interior face might have the most edges
            // so this could easily be improved upon
            int Perim = -1;
            int PerimCount = -1;
            for (int FE = 0; FE <= FaceEdges.Count - 1; FE++)
            {
                if (FaceEdges[FE] > PerimCount)
                {
                    Perim = FE;
                    PerimCount = FaceEdges[FE];
                }
            }
            DeleteEdges.Add(Perim);

            int NewPath = 0;
            DataTree<Curve> OutputFaces = new DataTree<Curve>();

            // only output those faces that haven't been identified as either the perimeter or open
            foreach (GH_Path Pth in F.Paths)
            {
                if (DeleteEdges.Contains(Pth.Indices(0)) == false)
                {
                    OutputFaces.AddRange(F.Branch(Pth), new GH_Path(NewPath));
                    NewPath += 1;
                }
            }
            CN = OutputFaces;
            */
            return HC;
        }


        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Access current selection
            Selection sel = uidoc.Selection;

            // Extraction of CurveElements by LineStyle WALL
            CurveElementFilter filter = new CurveElementFilter(CurveElementType.ModelCurve);
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> founds = collector.WherePasses(filter).ToElements();
            List<CurveElement> importCurves = new List<CurveElement>();

            foreach (CurveElement ce in founds)
            {
                importCurves.Add(ce);
            }
            var strayCurves = importCurves.Where(x => x.LineStyle.Name == "WALL").ToList();
            List<Curve> strayLines = new List<Curve>();
            foreach (CurveElement ce in strayCurves)
            {
                strayLines.Add(ce.GeometryCurve as Line);
            }


            // Grab the current building level
            FilteredElementCollector colLevels = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.INVALID)
                .OfClass(typeof(Level));
            Level firstLevel = colLevels.FirstElement() as Level;

            // Modify document within a transaction
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Generate Walls");

                List<Curve> shatters = Trim(strayLines);

                // Wall generation
                foreach (Curve shatter in shatters)
                {
                    Wall.Create(doc, shatter, firstLevel.Id, true);
                }

                tx.Commit();
            }

            return Result.Succeeded;
        }

        
    }
}
