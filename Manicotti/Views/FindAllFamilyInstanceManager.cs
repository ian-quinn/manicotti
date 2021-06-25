using Autodesk.Revit.DB;
using Manicotti.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Manicotti.Views
{
    class FindAllFamilyInstanceManager
    {
        ExternalDataWrapper Wrapper;
        public FindAllFamilyInstanceManager(ExternalDataWrapper wrapper)
        {
            Wrapper = wrapper;
        }
        public void Excetal()
        {
            List<Tuple<ElementId, string, int, string>> tuples = keyValuePair();
            List<Tuple<string, int, string, string>> tupleFam = keyValuePairFam(tuples);
            FindElement wPF_FindElement = new FindElement(tupleFam);
            wPF_FindElement.Show();
        }
        public List<Tuple<ElementId, string, int, string>> keyValuePair()
        {
            List<Tuple<ElementId, string, int, string>> keyValuePairs = new List<Tuple<ElementId, string, int, string>>();
            List<FamilyInstance> familyInstances = Familyinstances();
            foreach (var item in familyInstances)
            {
                string strName = item.Symbol.Name;
                ElementId elementId = item.Symbol.Id;
                string strA = string.Empty;
                try
                {
                    strA = item.Symbol.LookupParameter("Manufacture").AsString();
                }
                catch
                {
                    strA = "";
                }
                if (string.IsNullOrEmpty(strA))
                {
                    try

                    {
                        strA = item.Symbol.LookupParameter("生产商").AsString();
                    }
                    catch
                    {
                        strA = "";
                    }
                    
                }
                if (keyValuePairs.Find(t => t.Item1 == elementId) == null)
                {
                    keyValuePairs.Add(new Tuple<ElementId, string, int, string>(elementId, strName, 1, strA));
                }
                else
                {
                    Tuple<ElementId, string, int, string> tuple = keyValuePairs.Find(t => t.Item1 == elementId);

                    keyValuePairs.Add(new Tuple<ElementId, string, int, string>(tuple.Item1, tuple.Item2, tuple.Item3 + 1, strA));
                    keyValuePairs.Remove(tuple);
                }
            }
            return keyValuePairs;
        }

        public List<Tuple<string, int, string, string>> keyValuePairFam(List<Tuple<ElementId, string, int, string>> tuples)
        {
            List<Tuple<string, int, string, string>> keyValuePairFml = new List<Tuple<string, int, string, string>>();
            List<Tuple<ElementId, string, string, int, string>> keyValuePairs = new List<Tuple<ElementId, string, string, int, string>>();
            List<Tuple<ElementId, int>> elementIds = new List<Tuple<ElementId, int>>();
            foreach (var item in tuples)
            {
                FamilySymbol family = (Wrapper.Doc.GetElement(item.Item1) as FamilySymbol);
                string strName = family.Family.Name;
                ElementId elementId = family.Family.Id;
                keyValuePairs.Add(new Tuple<ElementId, string, string, int, string>(elementId, strName, item.Item2, item.Item3, item.Item4));
                if (elementIds.Find(t => t.Item1 == elementId) == null)
                {
                    elementIds.Add(new Tuple<ElementId, int>(elementId, item.Item3));
                }
                else
                {
                    Tuple<ElementId, int> tuple = elementIds.Find(t => t.Item1 == elementId);
                    elementIds.Add(new Tuple<ElementId, int>(tuple.Item1, tuple.Item2 + item.Item3));
                    elementIds.Remove(tuple);
                }

            }
            List<Tuple<ElementId, string, int, string>> keyValuePairsRemove = new List<Tuple<ElementId, string, int, string>>();

            foreach (var item in elementIds)
            {

                List<Tuple<ElementId, string, string, int, string>> keyValuePairsFam = keyValuePairs.FindAll(t => t.Item1 == item.Item1).ToList();
                for (int i = 0; i < keyValuePairsFam.Count; i++)
                {
                    if (i == 0)
                    {
                        keyValuePairFml.Add(new Tuple<string, int, string, string>(keyValuePairsFam[i].Item2, item.Item2, "族名称", ""));
                        keyValuePairFml.Add(new Tuple<string, int, string, string>(keyValuePairsFam[i].Item3, keyValuePairsFam[i].Item4, "族类型", keyValuePairsFam[i].Item5));
                    }
                    else
                    {
                        keyValuePairFml.Add(new Tuple<string, int, string, string>(keyValuePairsFam[i].Item3, keyValuePairsFam[i].Item4, "族类型", keyValuePairsFam[i].Item5));
                    }
                }
            }

            return keyValuePairFml;
        }


        public List<FamilyInstance> Familyinstances()
        {
            FilteredElementCollector elements = new FilteredElementCollector(Wrapper.Doc);
            List<FamilyInstance> familyInstances = elements.OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().ToList();
            return familyInstances;
        }
    }
}
