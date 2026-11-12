---
name: frontend-notifications
description: 'User feedback for mutations. When to toast, the notifySuccess/notifyError helpers, exact copy style, when NOT to toast, and the convention test that enforces it.'
---

# Frontend Notifications Skill

Every mutation must tell the user what happened. Before this convention existed, four of the app's
mutations completed **silently** — including a self-pay batch generation whose rejection was
entirely unhandled, and a client-note edit that reverted to the old text with no error, so the user
believed a rewritten clinical note had saved when it had not.

The rule is not "always show a toast." It is: **every mutation makes a deliberate decision about
user feedback, and that decision is visible in the code.**

## The helpers

Use `@/lib/notify` — never call `sonner`'s `toast` directly from a feature, and never build a second
notification path.

```ts
import { notifySuccess, notifyError } from '@/lib/notify';

export const useCreateClientNote = (clientId: string) => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateClientNoteRequest) => createClientNote(clientId, request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['client-notes', clientId] });
      notifySuccess('Note added.');
    },
    onError: (error) => {
      notifyError(error, 'Unable to add note right now. Please try again.');
    },
  });
};
```

`notifyError(error, fallback)` runs the error through `resolveErrorMessage`, which prefers a
ProblemDetails validation message, then `detail`, then `title`, then your fallback. So a server-side
validation message reaches the user verbatim, and a network failure still says something useful.

## Where the call goes

**In the hook, not the component.** One hook is consumed by many call sites; putting the toast in the
hook means every caller is covered and the copy cannot drift. It also survives a component being
refactored.

`useMutation`'s `onError` does **not** consume the rejection, so a caller that already `try/catch`es
`mutateAsync` keeps working unchanged.

## Copy style

| | |
|---|---|
| Success | Past tense, terse, no exclamation. `Note added.` `Referral submitted.` `Batch generated.` |
| Error fallback | What failed + what to do. `Unable to add note right now. Please try again.` |
| Never | Raw error text, stack traces, status codes, or IDs. Those go to `console.error`. |

Write for intake staff mid-task, not for a developer debugging.

## When NOT to toast

A toast for every background operation is noise, and noise trains people to dismiss the toasts that
matter. Do not toast:

- **Silent background checks** — duplicate/household candidate lookups, typeahead searches. The
  result *is* the feedback.
- **Actions whose result is already on screen** — if the row visibly updates, a toast is redundant.
- **Sensitive reveals** — e.g. revealing a masked SSN. Announcing it in a corner of the screen is
  the opposite of discreet.
- **Anything fired on a timer or by polling.** Never toast something the user did not initiate.

When you skip notification deliberately, say so where the mutation is defined:

```ts
// notify-exempt: background duplicate check; the candidate card is the feedback.
return useMutation({ ... });
```

That comment is what the convention test looks for. It is not a formality — it is how a reviewer
tells "considered and declined" apart from "forgot."

## Errors are the half that gets skipped

Success toasts get written because they are satisfying. `onError` gets skipped because the happy
path is what gets tested. Every real defect this convention exists to prevent was a **missing
`onError`**, not a missing success toast. If you write only one, write the error one.

## Enforcement

`frontend/src/lib/mutationNotifications.convention.test.ts` scans every `useMutation` in
`src/features/**` and fails if a mutation neither notifies nor carries a `notify-exempt` comment.

It ships with a **baseline** of files that predate the convention. The baseline is allowed to shrink
and must never grow — adding a file to it is a reviewer conversation, not a fix. New code has no
baseline and must comply.

## Related

`react-spa` (React Query patterns) · `frontend-testing` (asserting notifications with a mocked
`@/lib/notify`).
