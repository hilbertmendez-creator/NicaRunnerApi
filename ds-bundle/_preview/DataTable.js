var __dsPreview = (() => {
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
  var __reExport = (target, mod, secondTarget) => (__copyProps(target, mod, "default"), secondTarget && __copyProps(secondTarget, mod, "default"));
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

  // ds-raw:__ds_raw__
  var require_ds_raw = __commonJS({
    "ds-raw:__ds_raw__"(exports, module) {
      init_define_import_meta_env();
      module.exports = window.NicaRunnerUI;
    }
  });

  // shim:react-shim
  var require_react_shim = __commonJS({
    "shim:react-shim"(exports, module) {
      init_define_import_meta_env();
      var R = window.React;
      function jsx2(t, p, k) {
        return R.createElement(t, k === void 0 ? p : Object.assign({ key: k }, p));
      }
      module.exports = R;
      module.exports.jsx = jsx2;
      module.exports.jsxs = jsx2;
      module.exports.jsxDEV = jsx2;
      module.exports.Fragment = R.Fragment;
    }
  });

  // .design-sync/previews/DataTable.tsx
  var DataTable_exports = {};
  __export(DataTable_exports, {
    EmptyTable: () => EmptyTable,
    ResultsTable: () => ResultsTable
  });
  init_define_import_meta_env();

  // ds-shim:ds
  var ds_exports = {};
  __export(ds_exports, {
    default: () => ds_default
  });
  init_define_import_meta_env();
  __reExport(ds_exports, __toESM(require_ds_raw()));
  var g = window.NicaRunnerUI;
  var ds_default = "default" in g ? g.default : g;

  // .design-sync/previews/DataTable.tsx
  var import_jsx_runtime = __toESM(require_react_shim());
  var COLUMNS = [
    { header: "Pos.", render: (r) => r.position, className: "font-mono tabular-nums w-12" },
    { header: "Dorsal", render: (r) => r.number, className: "font-mono tabular-nums w-16" },
    { header: "Nombre", render: (r) => r.name },
    { header: "Categoría", render: (r) => r.category },
    { header: "Tiempo", render: (r) => r.finishTime, className: "font-mono tabular-nums" }
  ];
  var RUNNERS = [
    { id: 1, position: 1, number: 101, name: "Carlos Martínez", category: "Varonil Mayor", finishTime: "02:14:38" },
    { id: 2, position: 2, number: 215, name: "Pedro Gómez", category: "Varonil Mayor", finishTime: "02:19:55" },
    { id: 3, position: 3, number: 88, name: "Luis Herrera", category: "Varonil Mayor", finishTime: "02:23:11" },
    { id: 4, position: 1, number: 305, name: "Ana López", category: "Femenil Mayor", finishTime: "02:31:47" },
    { id: 5, position: 2, number: 412, name: "María Torres", category: "Femenil Mayor", finishTime: "02:38:09" }
  ];
  function ResultsTable() {
    return /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", { className: "p-4", children: /* @__PURE__ */ (0, import_jsx_runtime.jsx)(ds_exports.DataTable, { columns: COLUMNS, data: RUNNERS, rowKey: (r) => r.id }) });
  }
  function EmptyTable() {
    return /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", { className: "p-4", children: /* @__PURE__ */ (0, import_jsx_runtime.jsx)(
      ds_exports.DataTable,
      {
        columns: COLUMNS,
        data: [],
        rowKey: (r) => r.id,
        emptyState: /* @__PURE__ */ (0, import_jsx_runtime.jsx)(ds_exports.EmptyState, { message: "Sin resultados capturados todavía." })
      }
    ) });
  }
  return __toCommonJS(DataTable_exports);
})();
