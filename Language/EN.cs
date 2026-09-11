namespace SlimeKeyViewer
{
    internal static class EN
    {
        public static LangStrings Build() => new LangStrings
        {
            TabKeys             = "Keys",
            TabBehaviour        = "Motion",

            KeysHeading         = "{0} keys",
            Delete              = "Delete",
            Edit                = "Edit",
            AddKey              = "Add key",
            EditAllKeys         = "Edit all keys",
            Cancel              = "Cancel",
            CaptureHint         = "Every key you press is added.   Esc or Cancel to stop.",

            Back                = "← Back",
            AllKeys             = "All keys",
            OneKey              = "{0} key",

            GroupBox            = "Key box",
            GroupSlime          = "Slime",
            GroupLabelShadow    = "Label shadow",
            GroupJump           = "Jump",
            GroupParticle       = "Particles",
            GroupColorsIdle     = "Colours — released",
            GroupColorsPressed  = "Colours — pressed",
            GroupVisibility     = "Visibility",
            GroupPlacement      = "Placement",
            GroupDisplay        = "Display",

            Width               = "X",
            Height              = "Y",
            GapToNext           = "Gap to next key",
            CornerRadius        = "Corner radius",
            BorderWidth         = "Border width",
            FontSize            = "Font size",

            Size                = "Size",
            GapFromBox          = "Gap above box",
            Shadow              = "Shadow",
            OffsetX             = "Offset X",
            OffsetY             = "Offset Y",
            Opacity             = "Opacity",

            Distance            = "Distance",
            Softness            = "Softness",

            JumpOnPress         = "Jump the moment a key goes down",
            JumpHeight          = "Jump height",
            StepDuration        = "Step duration",
            FlipEachJump        = "Turn around each jump",
            FlipOnLanding       = "Turn on landing instead of on press",

            ParticleAtFeet      = "Burst from the slime's feet",

            ColorBorder         = "Border",
            ColorFill           = "Fill",
            ColorText           = "Text",

            VisibleOnlyOnInput  = "Show only while keying",
            FadeDuration        = "Fade duration",

            PosX                = "Horizontal position",
            PosY                = "Vertical position",
            Anchor              = "Anchor",
            OverallSize         = "Overall size",

            ShowViewer          = "Show viewer",
        };
    }
}
