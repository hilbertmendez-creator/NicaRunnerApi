/* @ds-bundle: {"namespace":"NicaRunnerUI","components":[{"name":"Button","sourcePath":"components/general/Button/Button.jsx"},{"name":"DataTable","sourcePath":"components/general/DataTable/DataTable.jsx"},{"name":"EmptyState","sourcePath":"components/general/EmptyState/EmptyState.jsx"},{"name":"ErrorAlert","sourcePath":"components/general/ErrorAlert/ErrorAlert.jsx"},{"name":"Input","sourcePath":"components/form/Input/Input.jsx"},{"name":"Label","sourcePath":"components/form/Label/Label.jsx"},{"name":"LoadingText","sourcePath":"components/general/LoadingText/LoadingText.jsx"},{"name":"MetricCard","sourcePath":"components/general/MetricCard/MetricCard.jsx"},{"name":"Modal","sourcePath":"components/general/Modal/Modal.jsx"},{"name":"Select","sourcePath":"components/form/Select/Select.jsx"},{"name":"Tabs","sourcePath":"components/general/Tabs/Tabs.jsx"},{"name":"Textarea","sourcePath":"components/form/Textarea/Textarea.jsx"}],"sourceHashes":{"components/general/Button/Button.jsx":"e63cd780de9e","components/general/Button/Button.d.ts":"2f81ce5f9a44","components/general/Button/Button.prompt.md":"db2c8323500f","components/general/DataTable/DataTable.jsx":"abb14db573d4","components/general/DataTable/DataTable.d.ts":"2492bc4c84f1","components/general/DataTable/DataTable.prompt.md":"6d9d87ec6155","components/general/EmptyState/EmptyState.jsx":"f09b37dbaa1c","components/general/EmptyState/EmptyState.d.ts":"9c7778405977","components/general/EmptyState/EmptyState.prompt.md":"382c714db52e","components/general/ErrorAlert/ErrorAlert.jsx":"8c4e662641ff","components/general/ErrorAlert/ErrorAlert.d.ts":"366abc382015","components/general/ErrorAlert/ErrorAlert.prompt.md":"e0651ce76adf","components/form/Input/Input.jsx":"54faf27f1577","components/form/Input/Input.d.ts":"7d084040c222","components/form/Input/Input.prompt.md":"bc64a5f6178b","components/form/Label/Label.jsx":"8bef33497056","components/form/Label/Label.d.ts":"36284751914b","components/form/Label/Label.prompt.md":"68d3d4c62799","components/general/LoadingText/LoadingText.jsx":"04ad9506cfd6","components/general/LoadingText/LoadingText.d.ts":"c32276f7a1cb","components/general/LoadingText/LoadingText.prompt.md":"6ea845d7d7b2","components/general/MetricCard/MetricCard.jsx":"18e834ad0bba","components/general/MetricCard/MetricCard.d.ts":"943920d767d5","components/general/MetricCard/MetricCard.prompt.md":"0b6c316d8835","components/general/Modal/Modal.jsx":"a8c2d2457f3b","components/general/Modal/Modal.d.ts":"d9bb7d328f7b","components/general/Modal/Modal.prompt.md":"374ba8c0b8ec","components/form/Select/Select.jsx":"41b303eb1653","components/form/Select/Select.d.ts":"32e11d3cc984","components/form/Select/Select.prompt.md":"95423ff6e98d","components/general/Tabs/Tabs.jsx":"eed472656697","components/general/Tabs/Tabs.d.ts":"c5bc603ce1a8","components/general/Tabs/Tabs.prompt.md":"bbcc5c730c04","components/form/Textarea/Textarea.jsx":"16c11a3a6d1e","components/form/Textarea/Textarea.d.ts":"19c7e9c6a318","components/form/Textarea/Textarea.prompt.md":"e57f9c693fe6"},"inlinedExternals":[],"builtBy":"cc-design-sync"} */
"use strict";
var NicaRunnerUI = (() => {
  var __create = Object.create;
  var __defProp = Object.defineProperty;
  var __getOwnPropDesc = Object.getOwnPropertyDescriptor;
  var __getOwnPropNames = Object.getOwnPropertyNames;
  var __getProtoOf = Object.getPrototypeOf;
  var __hasOwnProp = Object.prototype.hasOwnProperty;
  var __esm = (fn, res, err) => function __init() {
    if (err) throw err[0];
    try {
      return fn && (res = (0, fn[__getOwnPropNames(fn)[0]])(fn = 0)), res;
    } catch (e) {
      throw err = [e], e;
    }
  };
  var __commonJS = (cb, mod) => function __require() {
    try {
      return mod || (0, cb[__getOwnPropNames(cb)[0]])((mod = { exports: {} }).exports, mod), mod.exports;
    } catch (e) {
      throw mod = 0, e;
    }
  };
  var __export = (target, all) => {
    for (var name in all)
      __defProp(target, name, { get: all[name], enumerable: true });
  };
  var __copyProps = (to, from, except, desc) => {
    if (from && typeof from === "object" || typeof from === "function") {
      for (let key of __getOwnPropNames(from))
        if (!__hasOwnProp.call(to, key) && key !== except)
          __defProp(to, key, { get: () => from[key], enumerable: !(desc = __getOwnPropDesc(from, key)) || desc.enumerable });
    }
    return to;
  };
  var __toESM = (mod, isNodeMode, target) => (target = mod != null ? __create(__getProtoOf(mod)) : {}, __copyProps(
    // If the importer is in node compatibility mode or this is not an ESM
    // file that has been converted to a CommonJS file using a Babel-
    // compatible transform (i.e. "__esModule" has not been set), then set
    // "default" to the CommonJS "module.exports" for node compatibility.
    isNodeMode || !mod || !mod.__esModule ? __defProp(target, "default", { value: mod, enumerable: true }) : target,
    mod
  ));
  var __toCommonJS = (mod) => __copyProps(__defProp({}, "__esModule", { value: true }), mod);

  // <define:import.meta.env>
  var init_define_import_meta_env = __esm({
    "<define:import.meta.env>"() {
    }
  });

  // shim:react-shim
  var require_react_shim = __commonJS({
    "shim:react-shim"(exports, module) {
      init_define_import_meta_env();
      var R = window.React;
      function jsx13(t, p, k) {
        return R.createElement(t, k === void 0 ? p : Object.assign({ key: k }, p));
      }
      module.exports = R;
      module.exports.jsx = jsx13;
      module.exports.jsxs = jsx13;
      module.exports.jsxDEV = jsx13;
      module.exports.Fragment = R.Fragment;
    }
  });

  // frontend/packages/ui/dist/index.js
  var dist_exports = {};
  __export(dist_exports, {
    Button: () => Button,
    DataTable: () => DataTable,
    EmptyState: () => EmptyState,
    ErrorAlert: () => ErrorAlert,
    Input: () => Input,
    Label: () => Label,
    LoadingText: () => LoadingText,
    MetricCard: () => MetricCard,
    Modal: () => Modal,
    Select: () => Select,
    Tabs: () => Tabs,
    Textarea: () => Textarea
  });
  init_define_import_meta_env();
  var import_jsx_runtime = __toESM(require_react_shim(), 1);
  var import_react = __toESM(require_react_shim(), 1);
  var import_jsx_runtime2 = __toESM(require_react_shim(), 1);
  var import_jsx_runtime3 = __toESM(require_react_shim(), 1);
  var import_react2 = __toESM(require_react_shim(), 1);
  var import_jsx_runtime4 = __toESM(require_react_shim(), 1);
  var import_react3 = __toESM(require_react_shim(), 1);
  var import_jsx_runtime5 = __toESM(require_react_shim(), 1);
  var import_react4 = __toESM(require_react_shim(), 1);
  var import_jsx_runtime6 = __toESM(require_react_shim(), 1);
  var import_jsx_runtime7 = __toESM(require_react_shim(), 1);
  var import_jsx_runtime8 = __toESM(require_react_shim(), 1);
  var import_jsx_runtime9 = __toESM(require_react_shim(), 1);
  var import_jsx_runtime10 = __toESM(require_react_shim(), 1);
  var import_jsx_runtime11 = __toESM(require_react_shim(), 1);
  var import_jsx_runtime12 = __toESM(require_react_shim(), 1);
  var VARIANT_CLASSES = {
    primary: "bg-blue-700 text-white border border-blue-700 hover:bg-blue-800",
    secondary: "border border-zinc-200 text-zinc-700 hover:bg-zinc-50",
    destructive: "border border-critical-200 bg-critical-50 text-critical-600 hover:border-critical-600",
    info: "border border-official-200 bg-official-50 text-official-600 hover:border-official-600"
  };
  var SIZE_CLASSES = {
    sm: "h-6 px-2 text-xs",
    md: "h-8 px-3 text-sm"
  };
  function Button({
    variant = "secondary",
    size = "md",
    className = "",
    type = "button",
    ...rest
  }) {
    return /* @__PURE__ */ (0, import_jsx_runtime.jsx)(
      "button",
      {
        type,
        className: `font-medium disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-700 focus-visible:ring-offset-1 ${VARIANT_CLASSES[variant]} ${SIZE_CLASSES[size]} ${className}`,
        ...rest
      }
    );
  }
  var MAX_WIDTH_CLASSES = {
    md: "max-w-md",
    lg: "max-w-lg"
  };
  function Modal({ onClose, children, maxWidth = "md", labelledBy }) {
    const cardRef = (0, import_react.useRef)(null);
    (0, import_react.useEffect)(() => {
      const previouslyFocused = document.activeElement;
      const focusable = cardRef.current?.querySelector(
        "input, textarea, select, button"
      );
      focusable?.focus();
      function handleKeyDown(event) {
        if (event.key === "Escape") onClose();
      }
      document.addEventListener("keydown", handleKeyDown);
      return () => {
        document.removeEventListener("keydown", handleKeyDown);
        previouslyFocused?.focus();
      };
    }, [onClose]);
    return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(
      "div",
      {
        className: "fixed inset-0 flex items-center justify-center bg-black/30 p-4",
        onMouseDown: (e) => {
          if (e.target === e.currentTarget) onClose();
        },
        children: /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(
          "div",
          {
            ref: cardRef,
            role: "dialog",
            "aria-modal": "true",
            "aria-labelledby": labelledBy,
            className: `w-full ${MAX_WIDTH_CLASSES[maxWidth]} max-h-[90vh] overflow-y-auto p-6`,
            style: {
              background: "var(--bg-card, #ffffff)",
              border: "1px solid var(--bd-card, #e4e4e7)",
              borderRadius: "var(--radius-card, 0.5rem)"
            },
            children
          }
        )
      }
    );
  }
  function Label({ className = "", style, ...rest }) {
    return /* @__PURE__ */ (0, import_jsx_runtime3.jsx)(
      "label",
      {
        className: `mb-1 block text-sm font-medium ${className}`,
        style: { color: "var(--text-lo, #52525b)", ...style },
        ...rest
      }
    );
  }
  var Input = (0, import_react2.forwardRef)(
    function Input2({ className = "", invalid = false, ...rest }, ref) {
      return /* @__PURE__ */ (0, import_jsx_runtime4.jsx)(
        "input",
        {
          ref,
          "aria-invalid": invalid || void 0,
          className: `h-8 border bg-white px-3 text-sm text-zinc-900 focus:outline-none focus:ring-1 ${invalid ? "border-critical-600 focus:border-critical-600 focus:ring-critical-600" : "border-zinc-200 focus:border-blue-700 focus:ring-blue-700"} ${className}`,
          ...rest
        }
      );
    }
  );
  var Textarea = (0, import_react3.forwardRef)(
    function Textarea2({ className = "", invalid = false, ...rest }, ref) {
      return /* @__PURE__ */ (0, import_jsx_runtime5.jsx)(
        "textarea",
        {
          ref,
          "aria-invalid": invalid || void 0,
          className: `border bg-white px-3 py-2 text-sm text-zinc-900 focus:outline-none focus:ring-1 ${invalid ? "border-critical-600 focus:border-critical-600 focus:ring-critical-600" : "border-zinc-200 focus:border-blue-700 focus:ring-blue-700"} ${className}`,
          ...rest
        }
      );
    }
  );
  var Select = (0, import_react4.forwardRef)(
    function Select2({ className = "", invalid = false, ...rest }, ref) {
      return /* @__PURE__ */ (0, import_jsx_runtime6.jsx)(
        "select",
        {
          ref,
          "aria-invalid": invalid || void 0,
          className: `h-8 border bg-white px-3 text-sm text-zinc-900 focus:outline-none focus:ring-1 ${invalid ? "border-critical-600 focus:border-critical-600 focus:ring-critical-600" : "border-zinc-200 focus:border-blue-700 focus:ring-blue-700"} ${className}`,
          ...rest
        }
      );
    }
  );
  function LoadingText({ message = "Cargando...", className = "" }) {
    return /* @__PURE__ */ (0, import_jsx_runtime7.jsxs)("div", { role: "status", "aria-live": "polite", className: `flex items-center gap-3 py-4 ${className}`, children: [
      /* @__PURE__ */ (0, import_jsx_runtime7.jsx)("div", { className: "h-4 w-4 animate-spin rounded-full border-2 border-blue-700 border-t-transparent" }),
      /* @__PURE__ */ (0, import_jsx_runtime7.jsx)("span", { className: "animate-pulse text-sm font-medium text-zinc-500", children: message })
    ] });
  }
  function DataTable({
    columns,
    data,
    rowKey,
    emptyState,
    isLoading = false,
    loadingMessage
  }) {
    if (isLoading) {
      return /* @__PURE__ */ (0, import_jsx_runtime8.jsx)(LoadingText, { message: loadingMessage });
    }
    if (data.length === 0 && emptyState) {
      return /* @__PURE__ */ (0, import_jsx_runtime8.jsx)(import_jsx_runtime8.Fragment, { children: emptyState });
    }
    const labeledColumns = columns.filter((col) => col.header !== "");
    const actionColumns = columns.filter((col) => col.header === "");
    return /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)(import_jsx_runtime8.Fragment, { children: [
      /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("div", { className: "flex flex-col gap-2 sm:hidden", children: data.map((row) => /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)(
        "div",
        {
          className: "p-3",
          style: {
            background: "var(--bg-card, #ffffff)",
            border: "1px solid var(--bd-card, #e4e4e7)",
            borderRadius: "var(--radius-card, 0.5rem)"
          },
          children: [
            labeledColumns.map((col, idx) => /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("div", { className: "flex items-start justify-between gap-3 py-1 text-sm", children: [
              /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("span", { className: "text-xs uppercase tracking-wide", style: { color: "var(--text-th, #71717a)" }, children: col.header }),
              /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("span", { className: `text-right ${col.className ?? ""}`, style: { color: "var(--text-hi, #18181b)" }, children: col.render(row) })
            ] }, idx)),
            actionColumns.length > 0 && /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("div", { className: "mt-2 flex justify-end gap-2 border-t pt-2", style: { borderColor: "var(--bd-row, #e4e4e7)" }, children: actionColumns.map((col, idx) => /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("div", { children: col.render(row) }, idx)) })
          ]
        },
        rowKey(row)
      )) }),
      /* @__PURE__ */ (0, import_jsx_runtime8.jsx)(
        "div",
        {
          className: "hidden overflow-x-auto sm:block",
          style: { border: "1px solid var(--bd-card, #e4e4e7)", background: "var(--bg-card, #ffffff)" },
          children: /* @__PURE__ */ (0, import_jsx_runtime8.jsxs)("table", { className: "w-full border-collapse text-left text-sm", children: [
            /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("thead", { className: "sticky top-0", children: /* @__PURE__ */ (0, import_jsx_runtime8.jsx)(
              "tr",
              {
                className: "text-xs uppercase tracking-wide",
                style: {
                  borderBottom: "1px solid var(--bd, #e4e4e7)",
                  background: "var(--bg-th, #fafafa)",
                  color: "var(--text-th, #71717a)"
                },
                children: columns.map((col, idx) => /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("th", { scope: "col", className: `h-8 px-3 font-medium ${col.className ?? ""}`, children: col.header }, idx))
              }
            ) }),
            /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("tbody", { children: data.map((row) => /* @__PURE__ */ (0, import_jsx_runtime8.jsx)(
              "tr",
              {
                className: "row-hover h-9",
                style: { borderBottom: "1px solid var(--bd-row, #e4e4e7)", color: "var(--text-hi, #18181b)" },
                children: columns.map((col, idx) => /* @__PURE__ */ (0, import_jsx_runtime8.jsx)("td", { className: `px-3 align-middle ${col.className ?? ""}`, children: col.render(row) }, idx))
              },
              rowKey(row)
            )) })
          ] })
        }
      )
    ] });
  }
  var VARIANT_CLASSES2 = {
    gray: { bg: "bg-white border border-zinc-200", label: "text-zinc-500", value: "text-zinc-900" },
    orange: { bg: "bg-orange-50 border border-orange-300", label: "text-orange-800", value: "text-orange-800" },
    teal: { bg: "bg-official-50 border border-official-200", label: "text-official-700", value: "text-official-600" },
    amber: { bg: "bg-dispute-50 border border-dispute-200", label: "text-dispute-700", value: "text-dispute-600" },
    red: { bg: "bg-critical-50 border border-critical-200", label: "text-critical-700", value: "text-critical-600" }
  };
  var SIZE_CLASSES2 = {
    sm: { p: "p-2.5", label: "text-xs", value: "text-lg" },
    md: { p: "p-3", label: "text-xs", value: "text-xl" }
  };
  function MetricCard({ label, value, variant = "gray", size = "md", className = "" }) {
    const styles = VARIANT_CLASSES2[variant];
    const sizeStyles = SIZE_CLASSES2[size];
    return /* @__PURE__ */ (0, import_jsx_runtime9.jsxs)("div", { className: `${styles.bg} ${sizeStyles.p} ${className}`, children: [
      /* @__PURE__ */ (0, import_jsx_runtime9.jsx)("p", { className: `${styles.label} ${sizeStyles.label} mb-1 font-medium uppercase tracking-wide`, children: label }),
      /* @__PURE__ */ (0, import_jsx_runtime9.jsx)("p", { className: `${styles.value} ${sizeStyles.value} font-mono font-semibold tabular-nums`, children: value })
    ] });
  }
  function Tabs({ tabs, activeTab, onChange, className = "" }) {
    function handleKeyDown(event, index) {
      if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
      event.preventDefault();
      const delta = event.key === "ArrowRight" ? 1 : -1;
      const nextIndex = (index + delta + tabs.length) % tabs.length;
      const nextTab = tabs[nextIndex];
      onChange(nextTab.id);
      document.getElementById(`tab-${nextTab.id}`)?.focus();
    }
    return /* @__PURE__ */ (0, import_jsx_runtime10.jsx)("div", { role: "tablist", className: `flex gap-1 border-b border-zinc-200 ${className}`, children: tabs.map((tab, index) => {
      const isActive = tab.id === activeTab;
      return /* @__PURE__ */ (0, import_jsx_runtime10.jsx)(
        "button",
        {
          type: "button",
          role: "tab",
          id: `tab-${tab.id}`,
          "aria-selected": isActive,
          "aria-controls": `tabpanel-${tab.id}`,
          tabIndex: isActive ? 0 : -1,
          onClick: () => onChange(tab.id),
          onKeyDown: (event) => handleKeyDown(event, index),
          className: `-mb-px border-b-2 px-3 py-2 text-sm font-medium transition-colors duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-700 focus-visible:ring-offset-1 ${isActive ? "border-blue-700 text-blue-700" : "border-transparent text-zinc-500 hover:border-zinc-300 hover:text-zinc-800"}`,
          children: tab.label
        },
        tab.id
      );
    }) });
  }
  function EmptyState({ message, className = "", action }) {
    return /* @__PURE__ */ (0, import_jsx_runtime11.jsxs)("div", { className: `flex flex-col items-center justify-center border border-dashed border-zinc-200 bg-zinc-50 p-8 text-center ${className}`, children: [
      /* @__PURE__ */ (0, import_jsx_runtime11.jsx)("p", { className: "text-sm font-medium text-zinc-500", children: message }),
      action && /* @__PURE__ */ (0, import_jsx_runtime11.jsx)(Button, { variant: "secondary", size: "sm", className: "mt-3", onClick: action.onClick, children: action.label })
    ] });
  }
  function ErrorAlert({ message, className = "" }) {
    return /* @__PURE__ */ (0, import_jsx_runtime12.jsx)("div", { role: "alert", className: `border border-critical-200 bg-critical-50 p-4 ${className}`, children: /* @__PURE__ */ (0, import_jsx_runtime12.jsxs)("div", { className: "flex gap-2", children: [
      /* @__PURE__ */ (0, import_jsx_runtime12.jsx)("span", { className: "text-sm font-semibold text-critical-700", children: "Error:" }),
      /* @__PURE__ */ (0, import_jsx_runtime12.jsx)("p", { className: "text-sm font-medium text-critical-700", children: message })
    ] }) });
  }
  return __toCommonJS(dist_exports);
})();
window.NicaRunnerUI=NicaRunnerUI.__dsMainNs?Object.assign({},NicaRunnerUI,NicaRunnerUI.__dsMainNs,{__dsMainNs:undefined}):NicaRunnerUI;
