# Troubleshooting Guide

## Different Team authorization error

If the user sees a "Different Team" authorization error, explain:

Miro MCP authorization is team-specific. The user must authenticate against the team that owns the board.

Recommended steps:

1. Re-authenticate Miro MCP.
2. Select the team that contains the target board.
3. Confirm they can open the board directly in Miro.
4. For Enterprise accounts, confirm the admin enabled MCP for that team.

---

## MCP tools are missing

Ask:

1. Which MCP client are you using?
2. Did you install via plugin/marketplace or manual JSON config?
3. Are you using only one installation method?
4. Are you on a Miro Enterprise plan?
5. Has an admin enabled MCP for your team?

Recommended fixes:

- Use only one installation method per client.
- Prefer official plugin/marketplace integrations when available.
- Remove duplicate manual config if a plugin is already installed.
- Clear MCP tokens if the client supports it.
- Reconnect through OAuth.

---

## Invalid response during authentication

Recommended fix:

1. Clear Miro browser cookies.
2. Restart the OAuth flow.
3. Reconnect the MCP server.

---

## Permission error

Explain that Miro MCP respects existing Miro permissions.

Check:

1. Is the board URL correct?
2. Can the user open the board directly in Miro?
3. Does the user have edit permissions for write actions?
4. Did OAuth select the correct team?
