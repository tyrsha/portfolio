# 프로젝트 구조
- roslyn : C# Source Generator 및 Analyzer 구현 프로젝트입니다.
- sample : 기능 테스트를 위한 샘플 유니티 프로젝트입니다.

# Roslyn 분석기
분석기 기능 예제를 위해 Linq 사용시 경고를 보여주는 기능을 만들었습니다.

Unity 환경에서 Linq 사용은 GC(가비지 컬렉션) 오버헤드를 유발하여 성능 저하의 원인이 될 수 있기에, Linq 사용 시 경고를 제공하는 분석기를 제작했습니다.


# 코드 제너레이터
string으로 enum을 생성하는 코드를 생성하는 기능을 제작하였습니다.

C#의 기본 Enum.Parse 경우 리플렉션 기반으로 동작하여 반복 호출 시 성능 저하가 있습니다.

리플렉션을 사용하지 않는 (Source Generator 기반) String to Enum 변환 코드를 자동 생성하는 기능을 제작했습니다.

# 유니티 에디터 유틸리티
유니티 scene 화면에서 Ctrl + 마우스 우클릭을 이용하여 오브젝트를 찾는 예제를 제작하였습니다.

겹치는 오브젝트를 Hierchy를 포함하여 모두 보여줘서 겹쳐있는 오브젝트가 많은 경우 오브젝트를 찾기 좋습니다.

<img width="404" height="177" alt="image" src="https://github.com/user-attachments/assets/b19d8360-0b2c-45b8-a7ff-315be71bc41d" />
