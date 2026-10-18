# 라이선스와 외부 구성 요소

[English](THIRD-PARTY-NOTICES.md)

- SLCraft 클라이언트와 연결부 수정 사항: MIT, `LICENSE`.
- chasmlol/SkyCraft에서 파생된 Minecraft 연결부: MIT, `licenses/SkyCraft-MIT.txt`. 원본 저작권 고지는 소스에 유지합니다.
- `src/installer`의 별도 설치 호스트는 GPL-3.0-or-later이며 해당 폴더에 라이선스가 있습니다. `dependencies.json`에 고정한 커밋과 ZIP SHA256을 사용해 AnomalyLabsIO/anomaly-launcher의 Anomaly.InstallerCore를 내려받고 로컬에서 함께 빌드합니다. 설치 후 원본 GPL 소스·라이선스·의존성 선언·네이티브 도구 참조는 `.tools`에 남습니다. 배포 ZIP에는 컴파일된 Anomaly 구성 요소가 들어 있지 않습니다. [원본 프로젝트](https://github.com/AnomalyLabsIO/anomaly-launcher).
- Anomaly 리소스 스냅샷도 커밋으로 고정합니다. 설치 도구는 원본 매니페스트를 사용해 리소스 해시를 검증하고 설치 중 LavaGang의 릴리스에서 MelonLoader 0.7.2를 내려받습니다. 이 구성 요소에는 각각의 라이선스가 적용되며 SLCraft의 MIT 허가에 포함되지 않습니다.
- 재현 가능한 빌드를 위해 Gradle Wrapper를 Apache-2.0으로 포함합니다. `licenses/Apache-2.0.txt`를 확인하세요. Gradle, Fabric Loader, Fabric API, Minecraft 라이브러리, .NET은 설치 과정에서 각 구성 요소의 조건에 따라 내려받습니다.
- SCP:SL·Minecraft의 게임 실행 파일, 에셋, 텍스처, 계정 인증 정보, 저장 월드, 개발자 개인 설정을 배포하지 않습니다. 게임 사용에는 원래 배포사의 조건이 적용됩니다. 사용자는 지원되는 자기 게임 설치본과 Minecraft 사용 권한을 준비해야 합니다.

Windows 전용 실험적 오프라인 로컬 호스트 빌드입니다. 공개 서버나 원격 서버 모드를 지원한다고 안내하지 않습니다. 이 문서는 설명 번역이며 원본 라이선스 파일 자체를 대체하지 않습니다.
