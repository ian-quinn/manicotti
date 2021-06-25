﻿#region Header
//
// CmdInstallLocation.cs - determine Revit product install location
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace Manicotti.Util
{
    [Transaction(TransactionMode.ReadOnly)]
    public static class GetInstallPath
    {
        const string _reg_path_uninstall  = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        const string _reg_path_for_flavour  = @"SOFTWARE\Autodesk\Revit\Autodesk {0} {1}";

        public static string RegPathForFlavour(ProductType flavour, string version)
        {
            return string.Format(_reg_path_for_flavour, flavour, version);
        }

        /// <summary>
        /// Return a specific string value from a specific 
        /// subkey of a given registry key.
        /// </summary>
        /// <param name="reg_path_key">Registry key path</param>
        /// <param name="subkey_name">Subkey name.</param>
        /// <param name="value_name">Value name.</param>
        /// <returns>Registry string value.</returns>
        public static string GetSubkeyValue(string reg_path_key, string subkey_name, string value_name)
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(reg_path_key))
            {
                using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                {
                    return subkey.GetValue(value_name) as string;
                }
            }
        }

        public static string GetRevitProductCode(string reg_path_product)
        {
            return GetSubkeyValue(reg_path_product, "Components", "ProductCode");
        }

        public static string GetRevitInstallLocation(string product_code)
        {
            return GetSubkeyValue(_reg_path_uninstall, product_code, "InstallLocation");
        }

        public static string FormatData( string description, string version_name, string product_code, string install_location)
        {
            return string.Format("{0}: {1} \nProduct code: {2} \nInstall location: {3}", 
                description, version_name, product_code, install_location);
        }
        
        
        public static string Execute(Application app)
        {
            string reg_path_product  = RegPathForFlavour( app.Product, app.VersionNumber);

            string product_code = GetRevitProductCode(reg_path_product);

            string install_location = GetRevitInstallLocation(product_code);

            string msg = FormatData( "Running application", app.VersionName, product_code, install_location);

            foreach (ProductType p in Enum.GetValues(typeof(ProductType)))
            {
                try
                {
                    reg_path_product = RegPathForFlavour(p, app.VersionNumber);



                    product_code = GetRevitProductCode(reg_path_product);

                    install_location = GetRevitInstallLocation(product_code);

                    msg += FormatData(
                      "\n\nInstalled product",
                      p.ToString(),
                      product_code,
                      install_location);
                }
                catch (Exception)
                {
                }
            }

            return install_location;
        }
        
    }
}
