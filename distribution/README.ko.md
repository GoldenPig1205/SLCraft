# SLCraft Client 0.2.6 — 작은 배포본

SCP:SL 시설 안에서 Minecraft의 손·HUD·인벤토리·블록·엔티티·이동을 사용하는 Windows x64 실험 버전입니다. 0.1.0 서버 연동 시제품과 다른 **클라이언트 통합판**입니다. 지원 범위는 별도 오프라인 1인 로컬 호스트입니다.

## 먼저 준비할 것

- Windows 10/11 x64.
- Steam에서 설치한 **SCP:SL 14.2.7 Windows IL2CPP 원본**. 이 배포본은 검증한 GameAssembly.dll의 SHA256과 일치하는 설치본만 받습니다. 최신 버전이라는 이유만으로 다른 게임 빌드를 지원하지 않습니다.
- [Microsoft .NET SDK 10 이상](https://dotnet.microsoft.com/download/dotnet/10.0). 런타임만 설치하면 설치 도구를 빌드할 수 없습니다.
- [Windows x64 JDK 25 이상](https://adoptium.net/temurin/releases/). 설치 후 JAVA_HOME을 지정하거나 PATH에 Java를 등록하세요.
- 정식 Minecraft Java Edition 사용 권한. 현재 Minecraft 26.3을 Gradle 개발 환경으로 실행합니다. 일반 Minecraft 런처 계정·프로필을 가져오는 배포 방식은 아직 아닙니다.
- 인터넷 연결, 설치 과정에 약 12GB 이상의 여유 공간. ZIP은 작지만 실행에는 게임 사본·Java 라이브러리·텍스처·런타임이 필요합니다. 디스크 사용량은 설치 환경에 따라 늘어날 수 있습니다.

이 ZIP에는 SCP:SL 원본, Minecraft 원본·텍스처, 저장 월드, .NET/JDK, MelonLoader 또는 Anomaly 실행 바이너리가 들어 있지 않습니다. 사용자 PC의 SCP:SL을 복사하고 필요한 도구·의존성을 설치 중 내려받습니다. 작은 다운로드 파일이 전체 실행 용량을 없애 주는 것은 아닙니다.

## 설치

1. ZIP을 **게임 원본 폴더 밖의 쓰기 가능한 별도 폴더**에 풉니다. 전체 폴더를 유지하세요. `Program Files` 안에는 풀지 마세요.
2. 위 SDK와 JDK를 설치한 뒤 `Setup.cmd`를 실행합니다.
3. Steam 기본 설치 경로는 자동 검색합니다. 찾지 못하면 SCPSL.exe가 있는 원본 폴더 경로를 입력합니다. 다른 Steam 라이브러리라면 PowerShell에서 직접 지정할 수도 있습니다.

```powershell
.\Setup.ps1 -SLPath 'D:\SteamLibrary\steamapps\common\SCP Secret Laboratory'
```

설치 도구는 버전·파일 해시·경로 분리를 확인한 뒤, 고정된 Anomaly.InstallerCore 소스로 별도 설치 도구를 빌드합니다. Anomaly가 로컬 사본에 오프라인용 준비와 MelonLoader 설치를 수행합니다. Steam 원본에 덮어쓰지 않습니다. .NET 6.0.36은 이 사본용으로 따로 내려받습니다. 마지막으로 Gradle이 Minecraft와 Fabric 의존성을 준비합니다. 첫 설치·첫 실행은 인터넷 속도에 따라 오래 걸릴 수 있습니다.

준비가 끝나면 `Start-Client.cmd`를 실행합니다. 격리된 SL과 백그라운드 Minecraft가 시작되며, 로컬 호스트와 D계급을 만들고 자동으로 연결합니다. 처음에는 새 Minecraft 월드가 만들어집니다. 검사용 주민·물·TNT 자동 생성은 활성화하지 않습니다.

- 실행 중인 게임 재시작: `Restart-Client.cmd`. 저장 종료가 지연되면 강제 종료하지 않고 안내합니다.
- 설치 전 사전 검사만: `Setup.ps1 -Check -SLPath '원본 게임 폴더'`.
- 설치 엔진 dry run: `Setup.ps1 -DryRun -SLPath '원본 게임 폴더'`. 설치 도구 소스·빌드 캐시는 준비하지만 게임을 복사하거나 패치하지 않습니다.
- 인터넷/빌드 오류가 나면 메시지를 확인하고 같은 Setup 명령을 다시 실행하세요. 손상된 다운로드 캐시는 파일명을 바꾼 뒤 다시 시도하세요.
- 재압축하거나 GitHub에 올릴 때 `.local`, `.tools`, `src/minecraft/.gradle`, `build`, `run`, `bin`, `obj`와 로그는 제외하세요.

## 조작

| 입력 | 동작 |
| --- | --- |
| E | Minecraft 인벤토리 |
| T 또는 / | Minecraft 채팅·명령어 |
| M | SL 관리자 패널 열기·닫기 |
| ~ | SL 콘솔 |
| Esc | 메뉴 닫기 |
| F8 | Minecraft 연결 전환 |
| F10 | Minecraft 연결 해제 후 SL 조작 |

SL 관리자 메뉴가 열린 동안 Minecraft HUD가 잠시 숨겨지고 메뉴를 닫으면 복귀합니다. 연결 중 SL 인벤토리는 숨깁니다. 비행은 Minecraft에서 `/gamemode creative` 후 스페이스 두 번, 벽 통과는 `/gamemode spectator`를 사용하세요. 새 월드에 이전 개발자의 인벤토리나 저장 데이터는 포함되지 않습니다.

## 파일과 한계

- `payload/`: 현재 SLCraft DLL/JAR과 체크섬. 이 두 바이너리 외 게임 DLL은 배포하지 않습니다.
- `src/client`, `src/minecraft`, `src/tests`: SLCraft 소스와 빌드·검사 도구.
- `src/installer`: 로컬에서 Anomaly.InstallerCore와 함께 빌드하는 작은 설치 호스트.
- `.local/sl-runtime`: 설치 후 생성되는 개인용 SL 사본.
- `src/minecraft/run/saves`: 실행 후 만들어지는 개인 저장 월드.
- `.tools`: 설치 중 내려받은 도구 소스·압축 파일·설치 호스트.
- `dependencies.json`: 고정 버전, 다운로드 URL, 검사 해시. `payload/manifest.json`: SLCraft 바이너리 해시.

SL 시설 자체는 TNT로 파괴하지 않으며 설치한 Minecraft 블록에 폭발이 작용합니다. 시설 충돌은 0.25m 근사치입니다. 모든 문·경사·엘리베이터·역할이 검증된 상태는 아닙니다. 전투 연동과 원격 서버 플레이는 미완성입니다. 주민 얼굴과 물의 최신 육안 재검사는 아직 남아 있습니다.

문제 확인: `.local/minecraft-client.log`, `.local/sl-runtime/client-unity.log`, `.local/sl-runtime/MelonLoader/Latest.log`. 로그에는 PC 경로가 들어갈 수 있으므로 공개 전에 확인하세요.

## 소스 빌드

설치가 완료돼 Il2Cpp 참조가 생성된 뒤 실행합니다.

```powershell
dotnet build src/client/SLcraft.Client.csproj -c Release
dotnet run --project src/tests/Client.Protocol.Tests.csproj -c Release
cd src/minecraft
.\gradlew.bat build --no-daemon --console=plain
```

소스 빌드 결과는 자동으로 payload를 덮어쓰지 않습니다. 개발자가 바이너리를 갱신했다면 payload 체크섬도 새로 작성해야 합니다. 배포판은 0.2.6 바이너리를 유지합니다.

라이선스와 외부 구성 요소는 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)를 확인하세요. 이 프로젝트는 Northwood·Mojang·AnomalyLabs·SkyCraft의 공식 제품이 아닙니다.
