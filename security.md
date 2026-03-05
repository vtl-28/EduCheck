# Security Policy

## 🔒 Security Commitment

EduCheck is committed to maintaining the highest security standards to protect our users' data and ensure the integrity of our fraud detection platform. We take all security vulnerabilities seriously and appreciate the security research community's efforts in responsibly disclosing issues.

---

## 📋 Supported Versions

We release security updates for the following versions:

| Version | Supported | Status |
| ------- | --------- | ------ |
| 1.x.x (Current) | ✅ Yes | Active development |
| < 1.0 (Beta) | ❌ No | End of life |

---

## 🚨 Reporting a Vulnerability

### **DO NOT** Create Public Issues

**Please do not report security vulnerabilities through public GitHub issues, discussions, or pull requests.**

Security vulnerabilities could be exploited before a fix is deployed, potentially compromising user data. We ask that you report them privately so we can patch the issue before disclosure.

---

### How to Report

**Email:** vtlehola23@gmail.com  
**Subject Line:** `[SECURITY] Brief description of vulnerability`

### What to Include

Please provide as much information as possible to help us understand and reproduce the issue:

#### Required Information
- **Vulnerability Description:** Clear explanation of the security issue
- **Affected Component:** Which part of the system is vulnerable (API, frontend, infrastructure)
- **Attack Vector:** How the vulnerability can be exploited
- **Impact Assessment:** What an attacker could achieve (data access, privilege escalation, DoS, etc.)

#### Helpful Additional Information
- **Steps to Reproduce:** Detailed instructions to reproduce the vulnerability
- **Proof of Concept:** Code, screenshots, or video demonstration (if applicable)
- **Suggested Fix:** Your recommendations for remediation (optional)
- **Environment Details:** Browser, OS, or tools used to discover the issue
- **Your Contact Information:** For follow-up questions

### Example Report

```
Subject: [SECURITY] SQL Injection in Institute Search

Description:
The institute search endpoint appears vulnerable to SQL injection through 
the 'name' query parameter.

Affected Component:
- API Endpoint: POST /api/institutes/search
- File: EduCheck.API/Controllers/InstitutesController.cs

Attack Vector:
Submitting a search query with SQL metacharacters bypasses input validation:
Query: "university' OR '1'='1'; --"

Impact:
An attacker could:
- Extract sensitive database information
- Modify or delete records
- Gain unauthorized access to user data

Steps to Reproduce:
1. Navigate to search page
2. Enter payload: "test' OR '1'='1'; --"
3. Observe that all institutes are returned regardless of search term

Proof of Concept:
[Screenshot or video attached]

Suggested Fix:
Use parameterized queries or ORM-based queries to prevent SQL injection.

Environment:
- Browser: Chrome 120
- Date discovered: 2026-03-01
```

---

## ⏱️ Response Timeline

We are committed to responding quickly to security reports:

| Phase | Timeline | Description |
|-------|----------|-------------|
| **Initial Response** | Within 48 hours | Acknowledgment of receipt and initial assessment |
| **Validation** | Within 5 days | Confirm vulnerability and severity classification |
| **Status Update** | Within 7 days | Provide timeline for fix and deployment |
| **Fix Development** | Depends on severity | See severity timelines below |
| **Deployment** | After fix completion | Deploy patch to production |
| **Public Disclosure** | After deployment | Coordinate disclosure with reporter |

### Fix Development Timeline by Severity

| Severity | Target Fix Time | Description |
|----------|----------------|-------------|
| 🔴 **Critical** | 7 days | Immediate data breach risk, active exploitation possible |
| 🟠 **High** | 14 days | Significant security impact, but requires specific conditions |
| 🟡 **Medium** | 30 days | Moderate security impact with limited exploitation scenarios |
| 🟢 **Low** | 90 days | Minor security concern or theoretical vulnerability |

**Note:** These are target timelines. Complex vulnerabilities may require additional time, but we will keep you informed throughout the process.

---

## 🏆 Recognition & Thanks

### Security Hall of Fame

We maintain a [Security Hall of Fame](docs/SECURITY_HALL_OF_FAME.md) to publicly thank security researchers who responsibly disclose vulnerabilities. With your permission, we will:

1. Add your name (or pseudonym) to our Hall of Fame
2. Credit you in release notes when the fix is deployed
3. Provide a reference letter if requested (for security researchers building their portfolio)

**Current Contributors:** *(No vulnerabilities reported yet)*

### What We Offer

While EduCheck is currently a portfolio/beta project without a formal bug bounty program, we:

- ✅ Provide public recognition and credit
- ✅ Offer professional reference letters for security researchers
- ✅ Coordinate responsible disclosure timing
- ✅ Keep you updated throughout the remediation process

*Note: As the project scales and secures funding, we plan to launch a formal bug bounty program with monetary rewards.*

---

## 🔐 Security Best Practices for Contributors

If you're contributing code to EduCheck, please follow these security guidelines:

### For Developers

**Authentication & Authorization**
- ✅ Always validate user authentication (JWT) on protected endpoints
- ✅ Always check authorization (user owns resource) before data access
- ✅ Never trust client-side authentication checks
- ✅ Use `[Authorize]` attribute on all protected controllers

**Input Validation**
- ✅ Validate all user inputs on the server-side
- ✅ Use parameterized queries (Entity Framework) - never concatenate SQL
- ✅ Sanitize user input before storing in database
- ✅ Validate file uploads (type, size, content)

**Secrets Management**
- ✅ Never commit secrets, API keys, or passwords to Git
- ✅ Use environment variables for sensitive configuration
- ✅ Store secrets in AWS Parameter Store
- ✅ Use `.gitignore` to exclude sensitive files

**Output Encoding**
- ✅ Use Angular's built-in sanitization (don't bypass it)
- ✅ Set Content Security Policy headers
- ✅ Encode HTML output on the server-side
- ✅ Don't return sensitive data in error messages

**Session Management**
- ✅ Use short-lived JWT tokens (1 hour expiration)
- ✅ Store tokens in HttpOnly cookies (not localStorage)
- ✅ Invalidate tokens on logout
- ✅ Implement rate limiting on authentication endpoints

**Dependencies**
- ✅ Keep dependencies up to date
- ✅ Run `npm audit` and `dotnet list package --vulnerable` regularly
- ✅ Review security advisories for critical packages
- ✅ Don't use deprecated or unmaintained libraries

### For Code Reviewers

Before approving pull requests, verify:

- ✅ No hardcoded credentials or API keys
- ✅ Input validation present on all user-controlled data
- ✅ Authorization checks on all data access
- ✅ SQL queries use parameterized queries (Entity Framework)
- ✅ No sensitive data in error messages or logs
- ✅ HTTPS enforced for all external communications
- ✅ CSRF protection on state-changing operations
- ✅ Rate limiting on public endpoints

---

## 🛡️ Implemented Security Controls

EduCheck implements comprehensive security controls across all layers. For detailed information, see:

- **[Threat Model](docs/threat-model.md)** - Complete security analysis with 12 identified threats and mitigations
- **[Security Controls](docs/security-controls.md)** - Detailed implementation of security measures
- **[Security Testing](docs/security-testing.md)** - Testing methodology and results

### Security Highlights

**Application Security**
- ✅ SQL Injection prevention (Entity Framework parameterized queries)
- ✅ XSS prevention (Angular sanitization + CSP headers)
- ✅ CSRF protection (anti-forgery tokens)
- ✅ IDOR prevention (server-side authorization checks)
- ✅ Rate limiting (100 requests/minute per IP)

**Authentication & Authorization**
- ✅ JWT-based authentication (1-hour expiration)
- ✅ BCrypt password hashing (work factor 12)
- ✅ OAuth2 integration (Google)
- ✅ Role-Based Access Control (Student/Admin)
- ✅ Account lockout after 5 failed attempts

**Infrastructure Security**
- ✅ HTTPS/TLS 1.3 enforced with HSTS headers
- ✅ Secrets stored in AWS Parameter Store
- ✅ Container hardening (non-root users, minimal images)
- ✅ Network isolation with private subnets
- ✅ Automated security scanning in CI/CD

**Monitoring & Detection**
- ✅ Centralized logging (Grafana Loki)
- ✅ Security event tracking (failed logins, authorization failures)
- ✅ Real-time alerting for suspicious activity
- ✅ OpenTelemetry distributed tracing

---

## 🧪 Automated Security Testing

Our CI/CD pipeline includes comprehensive security scanning:

| Test Type | Tool | Frequency | Action on Findings |
|-----------|------|-----------|-------------------|
| **SAST** | Semgrep | Every commit | Block build on Critical/High |
| **Dependency Scan** | npm audit, dotnet check | Every build | Block on Critical/High CVEs |
| **Container Scan** | Trivy | Every image build | Block on Critical/High |
| **Secret Detection** | TruffleHog | Every commit | Block if secrets found |
| **Unit Tests** | xUnit, Playwright | Every commit | Block if tests fail |
| **Integration Tests** | TestContainers | Every build | Block if tests fail |

**Current Status:**
- 🟢 **0 Critical/High vulnerabilities** in production
- 🟢 **89 automated tests** passing (72 integration + 17 E2E)
- 🟢 **85% code coverage** including security-critical paths
- 🟢 **100% of identified threats** have mitigations implemented

---

## 📚 Security Resources

### External Resources
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [CWE Top 25 Most Dangerous Software Weaknesses](https://cwe.mitre.org/top25/)
- [NIST Cybersecurity Framework](https://www.nist.gov/cyberframework)

### EduCheck Documentation
- [Threat Model](docs/threat-model.md) - Complete threat analysis
- [Security Controls](docs/security-controls.md) - Implementation details
- [Security Testing](docs/security-testing.md) - Testing methodology
- [Contribution Guidelines](CONTRIBUTING.md) - Secure coding standards

---

## 🔄 Security Review Schedule

We conduct regular security reviews to maintain a strong security posture:

| Review Type | Frequency | Last Review | Next Review |
|-------------|-----------|-------------|-------------|
| **Threat Model Update** | Quarterly | March 2026 | June 2026 |
| **Dependency Audit** | Monthly | March 2026 | April 2026 |
| **Access Control Review** | Quarterly | March 2026 | June 2026 |
| **Penetration Testing** | Annually | March 2026 | March 2027 |
| **Security Training** | Semi-annually | - | September 2026 |

---

## 📞 Contact Information

### Security Team

**Primary Contact:**  
Vuyisile Lehola (Security Implementation Lead)  
📧 Email: vtlehola23@gmail.com  
🔗 GitHub: [@vtl-28](https://github.com/vtl-28)

**Response Time:**
- Emergency (active exploitation): Within 24 hours
- Critical severity: Within 48 hours
- All other reports: Within 5 business days

### Public Channels (For Non-Security Issues)

For general questions, feature requests, or non-security bugs:
- 📝 GitHub Issues: [github.com/vtl-28/EduCheck/issues](https://github.com/vtl-28/EduCheck/issues)
- 💬 GitHub Discussions: [github.com/vtl-28/EduCheck/discussions](https://github.com/vtl-28/EduCheck/discussions)

---

## ⚖️ Legal & Responsible Disclosure

### Safe Harbor

EduCheck considers security research conducted in accordance with this policy to be:

- ✅ Authorized under the Computer Fraud and Abuse Act (CFAA) and similar laws
- ✅ Exempt from legal action by the project maintainers
- ✅ Conducted in good faith security research

We will not pursue legal action against researchers who:

1. Follow responsible disclosure practices (private reporting)
2. Make good faith efforts to avoid:
   - Privacy violations (accessing other users' data beyond proof of concept)
   - Service disruption (DoS attacks, destructive actions)
   - Data destruction or modification
3. Do not exploit vulnerabilities beyond demonstrating their existence
4. Allow us reasonable time to patch before public disclosure

### Scope

**In Scope:**
- ✅ EduCheck web application (staging.educheck.org.za)
- ✅ EduCheck API (api.educheck.org.za)
- ✅ Public-facing infrastructure
- ✅ Authentication and authorization systems
- ✅ Data handling and storage

**Out of Scope:**
- ❌ Third-party services (Google OAuth, AWS, NeonDB)
- ❌ Social engineering or phishing attacks
- ❌ Physical security of servers or facilities
- ❌ Denial of Service (DoS/DDoS) attacks
- ❌ Spam or automated form submissions

### Prohibited Actions

Do **NOT**:
- ❌ Access or modify other users' data beyond proof of concept
- ❌ Execute DoS/DDoS attacks
- ❌ Perform social engineering or phishing
- ❌ Use automated vulnerability scanners that generate excessive traffic
- ❌ Publicly disclose vulnerabilities before they are patched
- ❌ Demand payment or threaten public disclosure

---

## 📈 Security Metrics (Public)

We believe in transparency about our security posture:

### Current Security Status

| Metric | Value | Status |
|--------|-------|--------|
| **Critical Vulnerabilities** | 0 | 🟢 |
| **High Vulnerabilities** | 0 | 🟢 |
| **Medium Vulnerabilities** | 0 | 🟢 |
| **Last Security Incident** | Never | 🟢 |
| **Threat Mitigation Rate** | 100% (12/12) | 🟢 |
| **Security Test Coverage** | 85% | 🟢 |
| **Average Response Time** | < 48 hours | 🟢 |

### Security Improvements Over Time

| Date | Improvement | Impact |
|------|-------------|--------|
| March 2026 | Comprehensive threat model created | Identified 12 threats, 100% mitigated |
| March 2026 | CI/CD security pipeline implemented | Automated SAST, DAST, secrets scanning |
| March 2026 | IDOR vulnerabilities fixed | Prevented unauthorized data access |
| March 2026 | Infrastructure hardening | Zero hardcoded secrets, TLS 1.3 |

---

## 🙏 Acknowledgments

We are grateful to the security research community for helping keep EduCheck secure. Special thanks to:

- **OWASP Foundation** - For comprehensive security resources and guidance
- **Semgrep Community** - For excellent SAST rules and documentation
- **GitHub Security Lab** - For security tooling and best practices

---

## 📄 Policy Updates

This security policy may be updated periodically to reflect new processes, tools, or contact information.

**Last Updated:** March 4, 2026  
**Version:** 1.0

---

## 🔗 Quick Links

- 📋 [Threat Model](docs/threat-model.md)
- 🛡️ [Security Controls](docs/security-controls.md)
- 🧪 [Security Testing](docs/security-testing.md)
- 🏆 [Security Hall of Fame](docs/SECURITY_HALL_OF_FAME.md)
- 🐛 [Report a Bug (Non-Security)](https://github.com/vtl-28/EduCheck/issues)
- 💬 [Community Discussions](https://github.com/vtl-28/EduCheck/discussions)

---

**Remember:** If you've found a security vulnerability, please email vtlehola23@gmail.com with subject `[SECURITY]` instead of creating a public issue. Thank you for helping keep EduCheck secure! 🔒