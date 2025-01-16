namespace Controllers
{
    public class RealignOnRelease : PseudoHapticController
    {
        public override string Name => "Realign On Release";
        protected override void Release()
        {
            base.Release();
            ResetDrift();
        }
    }
}
