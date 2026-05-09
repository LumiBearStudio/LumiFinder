# LumiFinder — 개인정보 처리방침

**최종 업데이트: 2026년 5월 9일**

<p align="center">
  <a href="../PRIVACY.md">English</a> |
  <strong>한국어</strong> |
  <a href="PRIVACY.ja.md">日本語</a> |
  <a href="PRIVACY.zh-CN.md">简体中文</a> |
  <a href="PRIVACY.zh-TW.md">繁體中文</a> |
  <a href="PRIVACY.de.md">Deutsch</a> |
  <a href="PRIVACY.es.md">Español</a> |
  <a href="PRIVACY.fr.md">Français</a> |
  <a href="PRIVACY.pt.md">Português</a>
</p>

---

## 개요

LumiFinder("본 앱")는 LumiBear Studio가 개발한 Windows용 파일 탐색기입니다. 저희는 사용자의 개인정보 보호에 최선을 다합니다. 본 정책은 어떤 데이터를 수집하고, 어떻게 보호하며, 어떻게 통제할 수 있는지 설명합니다.

## 수집하는 데이터

### 크래시 리포트 (Sentry)

본 앱은 자동 크래시 리포팅을 위해 [Sentry](https://sentry.io)를 사용합니다. 앱 충돌 또는 처리되지 않은 오류 발생 시 다음 데이터가 **전송될 수 있습니다**:

- **오류 세부 정보**: 예외 타입, 메시지, 스택 트레이스
- **장치 정보**: OS 버전, CPU 아키텍처, 충돌 시점 메모리 사용량
- **앱 정보**: 앱 버전, 런타임 버전, 빌드 구성

크래시 리포트는 **버그 식별 및 수정 목적으로만** 사용됩니다. 다음 항목은 **포함되지 않습니다**:

- 파일명, 폴더명, 파일 내용
- 사용자 계정 정보
- 탐색 히스토리 또는 경로
- 개인 식별 정보(PII)

### 크래시 리포트의 개인정보 보호 장치

크래시 리포트가 전송되기 전에 다층의 PII 스크러빙이 자동으로 적용됩니다:

- **사용자명 마스킹** — Windows 사용자 폴더 경로(`C:\Users\<사용자명>\...`)를 감지하여 사용자명 부분을 전송 전에 치환합니다. UNC 경로(`\\서버\공유\Users\<사용자명>\...`)에도 동일 적용됩니다.
- **`SendDefaultPii = false`** — Sentry SDK의 IP 주소, 서버 이름, 사용자 식별자 자동 수집을 완전히 비활성화했습니다.
- **파일 내용 미포함** — 스택 트레이스는 파일/폴더 내용을 포함하지 않으며, 라인 번호와 메서드 이름만 있습니다.

오픈소스 코드에서 직접 검증 가능합니다:
[`CrashReportingService.cs`](../src/LumiFiles/LumiFiles/Services/CrashReportingService.cs)

### 로컬 설정

본 앱은 사용자 환경설정(테마, 언어, 최근 폴더, 즐겨찾기, 커스텀 액센트 색상 등)을 Windows `ApplicationData.LocalSettings`를 통해 사용자 장치에 로컬로 저장합니다. 이 데이터는 **어떤 서버로도 전송되지 않습니다**.

## 수집하지 않는 데이터

- 개인정보 없음 (이름, 이메일, 주소)
- 파일 시스템 내용 또는 파일 메타데이터 없음
- 사용 분석 또는 텔레메트리 없음
- 위치 데이터 없음
- 광고 식별자 없음
- 마케팅 목적의 제3자 데이터 공유 없음

## 네트워크 접속

본 앱은 다음 용도로만 인터넷을 사용합니다:

- **크래시 리포팅** (Sentry) — 자동 오류 리포트, 비활성화 가능 (아래 "옵트아웃 방법" 참조)
- **FTP / FTPS / SFTP 연결** — 사용자가 명시적으로 설정한 경우에만
- **NuGet 패키지 복원** — 개발 빌드 시에만 (최종 사용자에서는 동작하지 않음)

## 크래시 리포팅 옵트아웃 방법

크래시 리포팅은 인터넷 연결을 끊지 않고도 앱 내에서 직접 비활성화할 수 있습니다:

1. **설정** 열기 (사이드바 좌측 하단)
2. **고급** 으로 이동
3. **크래시 리포팅** 토글 끄기

변경 사항은 즉시 적용됩니다. 옵트아웃 후에는 어떤 상황에서도 크래시 리포트가 전송되지 않습니다. 이미 Sentry 서버에 있는 과거 리포트는 표준 90일 보관 일정에 따라 만료됩니다.

## 데이터 저장 및 보관

- **Sentry 서버**: 크래시 리포트는 Sentry의 **독일 프랑크푸르트(EU)** 데이터 센터에 저장됩니다 — GDPR 준수 목적으로 선택. **90일** 후 자동 삭제됩니다.
- **로컬 설정**: 사용자 장치에만 저장. 앱 제거 시 함께 삭제됩니다.

## 데이터 처리자로서의 Sentry (GDPR)

Sentry는 GDPR에 따라 크래시 리포트의 데이터 처리자(Data Processor) 역할을 합니다. Sentry의 자체 개인정보 처리 관행과 보안 조치는 다음에서 확인 가능합니다:

- **Sentry 개인정보 처리방침**: [https://sentry.io/privacy/](https://sentry.io/privacy/)
- **Sentry 보안**: [https://sentry.io/security/](https://sentry.io/security/)
- **Sentry GDPR**: [https://sentry.io/legal/dpa/](https://sentry.io/legal/dpa/)

LumiBear Studio는 Sentry의 데이터 처리 약관을 검토했고, 크래시 데이터가 적절한 안전장치 없이 유럽경제지역(EEA)을 떠나지 않도록 EU 리전(`o4510949994266624.ingest.de.sentry.io`)을 선택했습니다.

## 아동 개인정보

본 앱은 13세 미만 아동의 데이터를 의도적으로 수집하지 않습니다. 본 앱은 아동을 대상으로 하지 않으며, 아동을 식별할 수 있는 어떤 개인정보도 수집하지 않습니다.

## 사용자 권리

저희는 개인 데이터를 수집하지 않으므로, 접근/수정/삭제할 개인 데이터가 없습니다. 구체적으로:

- **접근/이동권**: 해당 없음 — 저희가 보유한 개인 데이터 없음.
- **삭제권**: 해당 없음 — 저희가 보유한 개인 데이터 없음. 로컬 설정은 앱 제거로 제거 가능.
- **옵트아웃권**: 설정 > 고급 > 크래시 리포팅에서 사용 가능 (위 "옵트아웃 방법" 참조).

## 오픈소스

LumiFinder는 GPL v3.0 라이선스로 오픈소스입니다. 코드를 직접 검사/감사/수정할 수 있습니다:

- **소스 코드**: [https://github.com/LumiBearStudio/LumiFinder](https://github.com/LumiBearStudio/LumiFinder)
- **사용된 오픈소스 라이브러리**: [OpenSourceLicenses.md](../OpenSourceLicenses.md) 참조

## 연락

본 개인정보 처리방침에 대한 문의, 위반 발견, 위 권리 행사 요청:

- **GitHub Issues**: [https://github.com/LumiBearStudio/LumiFinder/issues](https://github.com/LumiBearStudio/LumiFinder/issues)
- **보안 신고**: [SECURITY.md](../SECURITY.md) 참조

## 본 정책의 변경

앱 발전 또는 법적 요구사항 변경에 따라 본 정책을 업데이트할 수 있습니다. 중요한 변경은 GitHub Releases를 통해 공지됩니다. 각 업데이트는 본 문서 상단의 "최종 업데이트" 날짜를 갱신합니다. 버전 히스토리는 [Git 히스토리](https://github.com/LumiBearStudio/LumiFinder/commits/main/PRIVACY.md)에서 영구 확인 가능합니다.
