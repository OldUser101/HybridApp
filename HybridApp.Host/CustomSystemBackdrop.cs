using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace HybridApp.Host
{
    public partial class CustomSystemBackdrop : SystemBackdrop
    {
        public enum ControllerMode 
        {
            Mica,
            Acrylic
        }

        MicaController? micaController;
        DesktopAcrylicController? acrylicController; 
        private readonly ControllerMode controlMode;
        ICompositionSupportsSystemBackdrop? connectedTarget = null;

        protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
        {
            base.OnTargetConnected(connectedTarget, xamlRoot);

            if (this.connectedTarget is not null) 
            {
                throw new Exception("This controller cannot be shared!.");
            }

            switch (controlMode)
            {
                case ControllerMode.Mica:
                    InitializeMicaController(connectedTarget, xamlRoot);
                    return;
                case ControllerMode.Acrylic:
                    InitializeAcrylicController(connectedTarget, xamlRoot);
                    return;
            }
        }

        private void InitializeMicaController(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot) 
        {
            if (micaController is not null)
            {
                throw new Exception("This controller cannot be shared");
            }

            micaController = new MicaController();
            SystemBackdropConfiguration defaultConfig = GetDefaultSystemBackdropConfiguration(connectedTarget, xamlRoot);
            micaController.SetSystemBackdropConfiguration(defaultConfig);
            micaController.AddSystemBackdropTarget(connectedTarget);

            this.connectedTarget = connectedTarget;
        }

        private void InitializeAcrylicController(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
        {
            if (acrylicController is not null)
            {
                throw new Exception("This controller cannot be shared");
            }

            acrylicController = new DesktopAcrylicController();
            SystemBackdropConfiguration defaultConfig = GetDefaultSystemBackdropConfiguration(connectedTarget, xamlRoot);
            acrylicController.SetSystemBackdropConfiguration(defaultConfig);
            acrylicController.AddSystemBackdropTarget(connectedTarget);

            this.connectedTarget = connectedTarget;
        }

        public CustomSystemBackdrop(ControllerMode mode) : base()
        {
            this.controlMode = mode;
        }

        public void SetControllerTintColor(Windows.UI.Color color) 
        {
            if (micaController is not null)
            {
                micaController.TintColor = color;
            }
            else if (acrylicController is not null) 
            {
                acrylicController.TintColor = color;
            }
        }

        public void SetControllerTintOpacity(float opacity)
        {
            if (micaController is not null)
            {
                micaController.TintOpacity = opacity;
            }
            else if (acrylicController is not null)
            {
                acrylicController.TintOpacity = opacity;
            }
        }

        protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
        {
            base.OnTargetDisconnected(disconnectedTarget);

            if (micaController is not null)
            {
                micaController.RemoveSystemBackdropTarget(disconnectedTarget);
                micaController = null;
                this.connectedTarget = null;
            }

            if (acrylicController is not null)
            {
                acrylicController.RemoveSystemBackdropTarget(disconnectedTarget);
                acrylicController = null;
                this.connectedTarget = null;
            }
        }

        public void Dispose()
        {
            if (micaController is not null)
            {
                // Perform cleanup for Mica
                if (connectedTarget is not null)
                {
                    micaController.RemoveSystemBackdropTarget(connectedTarget);
                }

                micaController = null;
            }

            if (acrylicController is not null)
            {
                // Perform cleanup for Acrylic
                if (connectedTarget is not null)
                {
                    acrylicController.RemoveSystemBackdropTarget(connectedTarget);
                }

                acrylicController = null;
            }
        }
    }
}
