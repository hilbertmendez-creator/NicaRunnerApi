Modal from @nicarunner/ui. Use via `window.NicaRunnerUI.Modal` (bundle loaded from the root `_ds_bundle.js`).

## Props

```ts
interface ModalProps {
  onClose: () => void;
  children: React.ReactNode;
  maxWidth?: "md" | "lg";
  labelledBy?: string;
}
```
