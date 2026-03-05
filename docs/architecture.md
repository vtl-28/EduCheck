# EduCheck Security Architecture Diagrams

This document contains comprehensive architecture diagrams showing EduCheck's security controls, trust boundaries, and threat mitigation strategies.

---

## System Architecture with Trust Boundaries
```mermaid
graph TB
    subgraph Internet["🌐 INTERNET (Untrusted Zone)"]
        User["👤 User Browser"]
        Attacker["⚠️ Potential Attacker"]
    end

    subgraph CDN["CloudFlare CDN (Optional)"]
        CF["DDoS Protection<br/>WAF"]
    end

    subgraph EC2["☁️ AWS EC2 Instance (DMZ - Trust Boundary 1)"]
        subgraph Frontend["Angular Frontend Container"]
            Angular["Angular 19 SPA<br/>Port 4200"]
        end
        
        subgraph ReverseProxy["Nginx Reverse Proxy"]
            Nginx["Nginx<br/>HTTPS/TLS 1.3<br/>Port 443"]
        end
        
        subgraph Backend["ASP.NET Backend Container"]
            API["ASP.NET Core API<br/>JWT Validation<br/>Port 5000"]
        end
    end

    subgraph AWS["☁️ AWS Services (Trusted Zone)"]
        Secrets["AWS Parameter Store<br/>🔐 Secrets Management"]
        ECR["AWS ECR<br/>Container Registry"]
    end

    subgraph Database["💾 Database Layer (Trust Boundary 3)"]
        DB["PostgreSQL<br/>NeonDB (Managed)<br/>TLS Encrypted"]
    end

    subgraph External["🔌 External Services"]
        Google["Google OAuth 2.0<br/>Authentication"]
        Grafana["Grafana Cloud<br/>📊 Monitoring"]
    end

    subgraph CICD["🔄 CI/CD Pipeline"]
        GitHub["GitHub Actions"]
        Semgrep["Semgrep SAST"]
        Trivy["Trivy Scanner"]
        TruffleHog["Secret Scanner"]
    end

    User -->|"HTTPS Request"| CF
    CF -->|"DDoS Protected"| Nginx
    Nginx -->|"Serve Static Files"| Angular
    Nginx -->|"Proxy API Requests<br/>/api/*"| API
    
    User -->|"OAuth Login"| Google
    Google -->|"Auth Token"| API
    API -->|"Issue JWT"| User
    
    API -->|"Parameterized Queries<br/>EF Core ORM"| DB
    
    API -.->|"Fetch Secrets<br/>(Runtime)"| Secrets
    
    API -->|"Logs & Metrics"| Grafana
    Frontend -->|"Error Tracking"| Grafana
    
    GitHub -->|"Code Commit"| Semgrep
    GitHub -->|"Container Build"| Trivy
    GitHub -->|"Git History"| TruffleHog
    GitHub -->|"Push Images"| ECR
    ECR -->|"Pull Images"| EC2
    
    Attacker -.->|"❌ SQL Injection<br/>(Blocked)"| API
    Attacker -.->|"❌ XSS Attack<br/>(Sanitized)"| Angular
    Attacker -.->|"❌ CSRF<br/>(Token Required)"| API
    Attacker -.->|"❌ Brute Force<br/>(Rate Limited)"| API
    Attacker -.->|"❌ IDOR<br/>(Authz Check)"| API

    classDef trusted fill:#d4edda,stroke:#28a745,stroke-width:3px
    classDef dmz fill:#fff3cd,stroke:#ffc107,stroke-width:3px
    classDef untrusted fill:#f8d7da,stroke:#dc3545,stroke-width:3px
    classDef security fill:#cfe2ff,stroke:#0d6efd,stroke-width:3px
    
    class DB,Secrets,ECR trusted
    class EC2,Frontend,Backend,ReverseProxy dmz
    class Internet,User,Attacker untrusted
    class CICD,Semgrep,Trivy,TruffleHog,Google,Grafana security
```

---

## Data Flow Diagram with Security Controls
```mermaid
flowchart LR
    subgraph Client["Client Layer"]
        Browser["User Browser"]
    end
    
    subgraph AppLayer["Application Layer"]
        FE["Angular Frontend<br/>───────────<br/>✓ Input Validation<br/>✓ XSS Sanitization<br/>✓ CSP Headers"]
        BE["ASP.NET API<br/>───────────<br/>✓ JWT Validation<br/>✓ RBAC Authorization<br/>✓ Rate Limiting<br/>✓ CSRF Protection"]
    end
    
    subgraph DataLayer["Data Layer"]
        Cache["Redis Cache<br/>───────────<br/>✓ Session Data<br/>✓ Rate Limit Store"]
        DB["PostgreSQL<br/>───────────<br/>✓ Encrypted Conn<br/>✓ Parameterized Queries"]
    end
    
    subgraph Security["Security Services"]
        OAuth["Google OAuth<br/>───────────<br/>✓ SSO Integration"]
        ParamStore["AWS Secrets<br/>───────────<br/>✓ JWT Secret<br/>✓ DB Credentials<br/>✓ API Keys"]
    end
    
    subgraph Monitoring["Monitoring & Logging"]
        Loki["Grafana Loki<br/>───────────<br/>✓ Security Events<br/>✓ Auth Failures<br/>✓ API Errors"]
    end

    Browser -->|"1. HTTPS Request<br/>(TLS 1.3)"| FE
    FE -->|"2. API Call<br/>(JWT Token)"| BE
    BE -->|"3. Validate Token"| ParamStore
    BE -->|"4. Check Cache"| Cache
    BE -->|"5. Query Data<br/>(ORM)"| DB
    BE -->|"6. Log Events"| Loki
    Browser -.->|"OAuth Login"| OAuth
    OAuth -.->|"Auth Code"| BE
    
    classDef client fill:#e7f3ff,stroke:#0066cc
    classDef app fill:#fff4e6,stroke:#ff9800
    classDef data fill:#e8f5e9,stroke:#4caf50
    classDef security fill:#fce4ec,stroke:#e91e63
    classDef monitor fill:#f3e5f5,stroke:#9c27b0
    
    class Browser client
    class FE,BE app
    class Cache,DB data
    class OAuth,ParamStore security
    class Loki monitor
```

---

## Threat Model STRIDE Mapping
```mermaid
mindmap
  root((EduCheck<br/>Threat Model))
    Spoofing
      T2: JWT Token Theft
        Mitigation: 1hr expiration
        Mitigation: HttpOnly cookies
        Mitigation: Signature validation
      T7: Brute Force
        Mitigation: Rate limiting
        Mitigation: Account lockout
        Mitigation: BCrypt hashing
    Tampering
      T1: SQL Injection
        Mitigation: Parameterized queries
        Mitigation: Entity Framework ORM
        Mitigation: Input validation
      T5: XSS Attacks
        Mitigation: Angular sanitization
        Mitigation: CSP headers
        Mitigation: Output encoding
      T8: CSRF
        Mitigation: Anti-forgery tokens
        Mitigation: SameSite cookies
    Repudiation
      Logging
        Mitigation: Grafana Loki
        Mitigation: Audit trail
        Mitigation: Security events
    Information Disclosure
      T4: Hardcoded Secrets
        Mitigation: AWS Parameter Store
        Mitigation: Environment variables
        Mitigation: TruffleHog scanning
      T10: Logs Exposure
        Mitigation: Sensitive data filtering
        Mitigation: Structured logging
      T12: Metrics Exposure
        Mitigation: Authentication required
        Mitigation: Minimal info disclosure
    Denial of Service
      T11: API Flooding
        Mitigation: Rate limiting
        Mitigation: Connection limits
        Mitigation: Request timeouts
    Elevation of Privilege
      T3: Privilege Escalation
        Mitigation: RBAC enforcement
        Mitigation: Role validation
        Mitigation: Server-side checks
      T6: IDOR
        Mitigation: Authorization checks
        Mitigation: Resource ownership validation
```

---

## Attack Surface Analysis
```mermaid
graph TD
    subgraph AttackSurface["🎯 Attack Surface"]
        A1["Public Web Interface<br/>───────────<br/>Risk: Medium<br/>Controls: 5"]
        A2["Authentication Endpoints<br/>───────────<br/>Risk: High<br/>Controls: 6"]
        A3["API Endpoints<br/>───────────<br/>Risk: High<br/>Controls: 8"]
        A4["Database Access<br/>───────────<br/>Risk: Critical<br/>Controls: 4"]
        A5["Admin Dashboard<br/>───────────<br/>Risk: High<br/>Controls: 5"]
    end
    
    subgraph Controls["🛡️ Security Controls"]
        C1["Input Validation"]
        C2["Output Encoding"]
        C3["Authentication"]
        C4["Authorization"]
        C5["Rate Limiting"]
        C6["Encryption"]
        C7["Logging"]
        C8["SAST/DAST"]
    end
    
    A1 --> C1
    A1 --> C2
    A1 --> C5
    A1 --> C7
    A1 --> C8
    
    A2 --> C3
    A2 --> C5
    A2 --> C6
    A2 --> C7
    A2 --> C8
    
    A3 --> C1
    A3 --> C3
    A3 --> C4
    A3 --> C5
    A3 --> C6
    A3 --> C7
    A3 --> C8
    
    A4 --> C3
    A4 --> C4
    A4 --> C6
    A4 --> C8
    
    A5 --> C3
    A5 --> C4
    A5 --> C5
    A5 --> C7
    A5 --> C8
    
    classDef high fill:#ffebee,stroke:#c62828,stroke-width:3px
    classDef medium fill:#fff3e0,stroke:#f57c00,stroke-width:2px
    classDef control fill:#e8f5e9,stroke:#2e7d32,stroke-width:2px
    
    class A2,A3,A5 high
    class A1 medium
    class A4 high
    class C1,C2,C3,C4,C5,C6,C7,C8 control
```

---

## Security Testing Coverage
```mermaid
pie title Security Testing Coverage by Category
    "Authentication & Authorization" : 28
    "Input Validation & Injection" : 24
    "Session Management" : 12
    "Cryptography & Secrets" : 10
    "Infrastructure & Config" : 15
    "Monitoring & Logging" : 11
```

---

## Deployment Security Pipeline
```mermaid
sequenceDiagram
    participant Dev as Developer
    participant Git as GitHub
    participant SAST as Semgrep SAST
    participant Deps as Dependency Scanner
    participant Secrets as TruffleHog
    participant Build as Docker Build
    participant Trivy as Trivy Scanner
    participant ECR as AWS ECR
    participant EC2 as AWS EC2

    Dev->>Git: 1. Push Code
    Git->>SAST: 2. Run SAST Scan
    SAST-->>Git: ✓ No vulnerabilities
    Git->>Deps: 3. Check Dependencies
    Deps-->>Git: ✓ No CVEs
    Git->>Secrets: 4. Scan for Secrets
    Secrets-->>Git: ✓ No secrets found
    Git->>Build: 5. Build Docker Images
    Build->>Trivy: 6. Scan Image
    Trivy-->>Build: ✓ No critical vulns
    Build->>ECR: 7. Push Secure Image
    ECR->>EC2: 8. Deploy to Production
    EC2-->>Dev: ✓ Deployment Success
    
    Note over SAST,Secrets: Security Gates<br/>Block on Critical/High
    Note over ECR,EC2: Only Vetted Images<br/>Reach Production
```

---

## How to Use These Diagrams

### In Your Documentation

These diagrams support the comprehensive security documentation in this repository:

- **[System Architecture](THREAT-MODEL.md#system-overview)** - Shows trust boundaries and component interactions
- **[Data Flow](THREAT-MODEL.md#security-controls-by-layer)** - Illustrates security controls at each layer
- **[STRIDE Mapping](THREAT-MODEL.md#identified-threats)** - Visual representation of all 12 threats and mitigations
- **[Attack Surface](THREAT-MODEL.md#risk-assessment-matrix)** - Risk levels and control coverage
- **[Testing Coverage](THREAT-MODEL.md#security-testing-results)** - Distribution of 89 security tests
- **[Deployment Pipeline](THREAT-MODEL.md#mitigation-status)** - 10-stage security scanning process

### For Interviews and Presentations

Use these diagrams when discussing:
- System architecture and design decisions
- Defense-in-depth security strategy
- Threat modeling methodology (STRIDE)
- CI/CD security integration
- Risk assessment and prioritization

---

## Additional Resources

- **[Complete Threat Model](THREAT-MODEL.md)** - Detailed analysis of all 12 threats
- **[Security Policy](../SECURITY.md)** - Vulnerability reporting guidelines
- **[Project README](../README.md)** - Overview and security highlights

---

**Last Updated:** March 2026  
**Maintained by:** Vuyisile Lehola