# 프로젝트 구조
- roslyn : C# Source Generator 및 Analyzer 구현 프로젝트입니다.
- sample : 기능 테스트를 위한 샘플 유니티 프로젝트입니다.

# Roslyn 분석기
분석기 기능 예제를 위해 Linq 사용시 경고를 보여주는 기능을 만들었습니다.

Unity 환경에서 Linq 사용은 GC(가비지 컬렉션) 오버헤드를 유발하여 성능 저하의 원인이 될 수 있기에, Linq 사용 시 경고를 제공하는 분석기를 제작했습니다.

## 사용 방법
코드를 작성하면 자동으로 LINQ 사용 시 경고가 표시됩니다. Diagnostic ID: `TEST001`

```csharp
// ⚠️ 경고가 표시됩니다
var result = list.Where(x => x > 0).ToList();
```

# 코드 제너레이터
string으로 enum을 생성하는 코드를 생성하는 기능을 제작하였습니다.

C#의 기본 Enum.Parse 경우 리플렉션 기반으로 동작하여 반복 호출 시 성능 저하가 있습니다.

리플렉션을 사용하지 않는 (Source Generator 기반) String to Enum 변환 코드를 자동 생성하는 기능을 제작했습니다.

## 사용 방법
1. Enum에 `[EnumLookup]` 어트리뷰트를 추가합니다:

```csharp
using RoslynCommon;

[EnumLookup]
public enum AssetCategory 
{
    Texture,
    Binary,
    Audio,
}
```

2. 빌드 후 자동 생성된 확장 메서드를 사용합니다:

```csharp
string categoryName = "Texture";
AssetCategory category = categoryName.ToAssetCategory(); // 리플렉션 없이 빠른 변환
```

## 생성되는 코드
Source Generator가 다음과 같은 확장 메서드를 자동 생성합니다:

```csharp
namespace EnumLookup.Generated
{
    public static class AssetCategoryExtensions
    {
        private static Dictionary<string, AssetCategory> dict = new() 
        {
            {"Texture", AssetCategory.Texture},
            {"Binary", AssetCategory.Binary},
            {"Audio", AssetCategory.Audio},
        }; 
    
        public static AssetCategory ToAssetCategory(this string name)
        {
            if (dict.TryGetValue(name, out var value))
            {
                return value;
            }
            return default;
        }
    }
}
```

# 유니티 에디터 유틸리티
유니티 scene 화면에서 Ctrl + 마우스 우클릭을 이용하여 오브젝트를 찾는 예제를 제작하였습니다.

겹치는 오브젝트를 Hierchy를 포함하여 모두 보여줘서 겹쳐있는 오브젝트가 많은 경우 오브젝트를 찾기 좋습니다.

## 사용 방법
1. Unity 에디터에서 씬 뷰를 엽니다.
2. **Ctrl + 마우스 우클릭**을 누릅니다.
3. 겹쳐있는 모든 오브젝트가 트리 뷰로 표시됩니다.
4. 원하는 오브젝트를 클릭하여 선택합니다.

<img width="404" height="177" alt="image" src="https://github.com/user-attachments/assets/b19d8360-0b2c-45b8-a7ff-315be71bc41d" />

# 빌드 및 설치

## 요구사항
- **Unity**: 2022.3.62f3 이상 (또는 유사한 버전)
- **.NET**: .NET Standard 2.0
- **C#**: C# 9.0 이상
- **Visual Studio / Rider**: Roslyn 프로젝트 빌드용

## 빌드 방법

**주의**: 빌드 순서가 중요합니다. `RoslynCommon`을 먼저 빌드해야 합니다.

```bash
# 1. RoslynCommon 프로젝트 빌드
cd roslyn/RoslynCommon
dotnet build

# 2. RoslynAnalyzer 프로젝트 빌드
cd ../RoslynAnalyzer
dotnet build
```

또는 솔루션 전체 빌드:

```bash
cd roslyn
dotnet build roslyn.sln
```

빌드가 완료되면 자동으로 `sample/Assets/roslyn/` 디렉토리에 다음 DLL 파일들이 복사됩니다:
- `RoslynAnalyzer.dll` - Analyzer 및 Source Generator
- `RoslynCommon.dll` - 공통 Attribute
- `Scriban.dll` - 템플릿 엔진 (의존성)

> **참고**: Post-build 이벤트가 Windows의 `copy` 명령어를 사용하므로, Windows 환경에서 빌드해야 합니다. 다른 플랫폼에서는 수동으로 DLL 파일을 복사해야 합니다.

## Unity 프로젝트 설정
1. Unity 에디터에서 `sample` 프로젝트를 엽니다.
2. 빌드된 DLL 파일들이 `Assets/roslyn/` 폴더에 있는지 확인합니다.
3. Unity가 자동으로 Analyzer를 인식합니다.

# 기술 스택
- **Roslyn**: C# 컴파일러 플랫폼
  - `Microsoft.CodeAnalysis.CSharp` (v3.8.0)
- **Scriban**: 템플릿 엔진 (v6.5.0)
- **Unity**: 게임 엔진 (2022.3.62f3)
- **.NET Standard 2.0**: 타겟 프레임워크


# 트러블슈팅

## Source Generator가 코드를 생성하지 않는 경우
1. **빌드 확인**: `RoslynCommon`과 `RoslynAnalyzer` 프로젝트가 모두 빌드되었는지 확인합니다.
2. **DLL 위치**: `sample/Assets/roslyn/` 폴더에 모든 DLL 파일이 있는지 확인합니다.
3. **Attribute 확인**: Enum에 `[EnumLookup]` 어트리뷰트가 올바르게 추가되었는지 확인합니다.
4. **로그 확인**: `sample/roslyn_log.txt` 파일을 확인하여 Source Generator 실행 여부를 확인합니다.
5. **Unity 재시작**: Unity 에디터를 재시작하여 Analyzer를 다시 로드합니다.

## LINQ 분석기가 작동하지 않는 경우
1. **Analyzer DLL 확인**: `Assets/roslyn/RoslynAnalyzer.dll` 파일이 있는지 확인합니다.
2. **Unity 재컴파일**: Unity가 스크립트를 다시 컴파일할 때까지 기다립니다.
3. **IDE 확인**: Visual Studio나 Rider에서도 Analyzer가 작동하는지 확인합니다.

# 추가 정보

## 디버깅 및 로깅
Source Generator는 실행 시 `sample/roslyn_log.txt` 파일에 로그를 기록합니다. 문제 해결 시 이 파일을 확인하세요.
