namespace CapaVista_Contabilidad.Properties {
    using System;

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {

        private static global::System.Resources.ResourceManager resourceMan;
        private static global::System.Globalization.CultureInfo resourceCulture;

        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("CapaVista_Contabilidad.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }

        internal static System.Drawing.Bitmap btn_revisarPeriodo {
            get {
                object obj = ResourceManager.GetObject("btn_revisarPeriodo", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }

        internal static System.Drawing.Bitmap btn_cerrarPeriodo {
            get {
                object obj = ResourceManager.GetObject("btn_cerrarPeriodo", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }

        internal static System.Drawing.Bitmap nav_inicio {
            get {
                object obj = ResourceManager.GetObject("nav_inicio", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }

        internal static System.Drawing.Bitmap nav_anterior {
            get {
                object obj = ResourceManager.GetObject("nav_anterior", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }

        internal static System.Drawing.Bitmap nav_siguiente {
            get {
                object obj = ResourceManager.GetObject("nav_siguiente", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }

        internal static System.Drawing.Bitmap nav_fin {
            get {
                object obj = ResourceManager.GetObject("nav_fin", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }

        internal static System.Drawing.Icon Icono_Contabilidad {
            get {
                object obj = ResourceManager.GetObject("Icono_Contabilidad", resourceCulture);
                return ((System.Drawing.Icon)(obj));
            }
        }
    }
}