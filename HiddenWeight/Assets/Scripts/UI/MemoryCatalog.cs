using System.Collections.Generic;

namespace HiddenWeight.UI
{
    // 저장에는 안정적인 기술 ID를 남기고 화면에는 정서적 제목을 보여 준다.
    // ID 접두사는 지역별 기억 실을 뜻해 카드 정렬과 연결선에도 사용한다.
    public static class MemoryCatalog
    {
        static readonly Dictionary<string, string> Titles = new Dictionary<string, string>
        {
            { "residue_s1", "손가락 아래의 목소리" },
            { "residue_skill", "되돌릴 수 있는 것" },
            { "residue_r11", "쌓인 무게" },
            { "residue_final", "남겨 둔 문" },
            { "gaze_g04", "등을 돌린 자리" },
            { "gaze_gs1", "보이지 않는 관객" },
            { "gaze_skill", "눈을 뜨는 연습" },
            { "gaze_g11", "판결 전의 침묵" },
            { "gaze_final", "나를 보는 나" },
            { "gaze_core", "시선의 중심" },
            { "fracture_f04", "흔들리는 쪽" },
            { "fracture_fs1", "고르지 않은 문" },
            { "fracture_skill", "아직 오지 않은 것" },
            { "fracture_f11", "완성되지 않은 폐허" },
            { "fracture_final", "선택한 내일" },
            { "fracture_core", "균열 너머" },
        };

        public static bool Has(string id) => !string.IsNullOrEmpty(id) && Titles.ContainsKey(id);
        public static string TitleFor(string id) => Has(id) ? Titles[id] : "이름 없는 기억";
        public static string RegionFor(string id)
        {
            if (id != null && id.StartsWith("residue_")) return "과거 · 잔재";
            if (id != null && id.StartsWith("gaze_")) return "현재 · 응시";
            if (id != null && id.StartsWith("fracture_")) return "미래 · 균열";
            return "겹쳐진 기억";
        }
        public static string SortKey(string id) => RegionFor(id) + "/" + (id ?? string.Empty);
    }
}
