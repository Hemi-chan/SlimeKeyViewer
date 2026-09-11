namespace SlimeKeyViewer
{
    internal static class KR
    {
        public static LangStrings Build() => new LangStrings
        {
            TabKeys             = "키",
            TabBehaviour        = "동작",

            KeysHeading         = "키 {0}개",
            Delete              = "삭제",
            Edit                = "편집",
            AddKey              = "키 추가",
            EditAllKeys         = "모든 키 편집",
            Cancel              = "취소",
            CaptureHint         = "누르는 키가 차례로 추가됩니다.   Esc 또는 취소 버튼으로 종료",

            Back                = "← 뒤로",
            AllKeys             = "모든 키",
            OneKey              = "{0} 키",

            GroupBox            = "키 박스",
            GroupSlime          = "Slime",
            GroupLabelShadow    = "글자 그림자",
            GroupJump           = "점프",
            GroupParticle       = "파티클",
            GroupColorsIdle     = "색상 — 뗐을 때",
            GroupColorsPressed  = "색상 — 눌렀을 때",
            GroupVisibility     = "표시 방식",
            GroupPlacement      = "배치",
            GroupDisplay        = "표시",

            Width               = "X",
            Height              = "Y",
            GapToNext           = "다음 키와의 간격",
            CornerRadius        = "모서리 둥글기",
            BorderWidth         = "테두리 두께",
            FontSize            = "글자 크기",

            Size                = "크기",
            GapFromBox          = "박스와의 간격",
            Shadow              = "그림자",
            OffsetX             = "X",
            OffsetY             = "Y",
            Opacity             = "진하기",

            Distance            = "거리",
            Softness            = "번짐",

            JumpOnPress         = "누르는 순간 바로 점프",
            JumpHeight          = "점프 높이",
            StepDuration        = "단계 시간",
            FlipEachJump        = "점프마다 좌우반전",
            FlipOnLanding       = "누를 때 대신 착지할 때 반전",

            ParticleAtFeet      = "슬라임 발밑에서 터뜨리기",

            ColorBorder         = "테두리",
            ColorFill           = "배경",
            ColorText           = "글자",

            VisibleOnlyOnInput  = "키 누를 때만 보이기",
            FadeDuration        = "페이드 시간",

            PosX                = "가로 위치",
            PosY                = "세로 위치",
            Anchor              = "중심점",
            OverallSize         = "전체 크기",

            ShowViewer          = "뷰어 표시",
        };
    }
}
