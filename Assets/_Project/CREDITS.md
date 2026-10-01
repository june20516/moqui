# 크레딧

외부 에셋은 한 행씩 기록한다 (`tech/asset-pipeline.md`). 허용 라이선스: CC0, CC-BY 3.0/4.0, MIT, Apache-2.0, Unity Companion License.
`Assets/_Project/Art`, `Audio` 아래 `Generated/` 밖의 모든 파일은 이 표에 있어야 한다 (EditMode `CreditsTests`가 검사).

## 외부 에셋

| 에셋 경로 | 원본 이름 | 제작자 | 출처 URL | 라이선스 | 확인일 | 변경 사항 |
|---|---|---|---|---|---|---|

현재 외부 에셋은 없다.

## 자체 제작물 (`Generated/`)

코드로 생성했으며 외부 저작물을 포함하지 않는다.

| 경로 | 생성기 |
|---|---|
| `Art/Generated/web_grid.png` | `Scripts/Editor/SandboxSceneBuilder.cs` (거미줄 격자 텍스처) |
| `Art/Generated/Moki/` | `Scripts/Editor/MokiBuilder.cs` (모키 애니메이션 클립 7종·컨트롤러) |
| `Audio/Generated/` | `tools/gen_audio.py` (효과음·환경음·음악 31종 합성) |

셰이더(`Shaders/`)와 모키·인간·가구 모델(프리미티브 조합)도 프로젝트에서 직접 작성했다.
글꼴은 번들하지 않고 실행 중 OS 글꼴을 쓴다 (D-041).
