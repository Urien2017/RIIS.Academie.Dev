import fs from "node:fs/promises";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const inputPath = "D:/Downloads/Chrome/canevas-notes-2026-2027-2026-2027-bts-n1-bat-s1-classe-kribi (4).xlsx";
const input = await FileBlob.load(inputPath);
const workbook = await SpreadsheetFile.importXlsx(input);

const summary = await workbook.inspect({
  kind: "workbook,sheet,table",
  maxChars: 12000,
  tableMaxRows: 8,
  tableMaxCols: 20,
  tableMaxCellChars: 120,
});
console.log("SUMMARY");
console.log(summary.ndjson);

const sheet = workbook.worksheets.getItem("Grille_Saisie");
const used = sheet.getUsedRange();
console.log("USED_RANGE");
console.log(used?.address ?? "none");
const region = await workbook.inspect({
  kind: "region",
  sheetId: "Grille_Saisie",
  range: used?.address ?? "A1:Z30",
  maxChars: 30000,
});
console.log("REGION");
console.log(region.ndjson);
console.log("VALUES_A7_AM11");
console.log(JSON.stringify(sheet.getRange("A7:AM11").values));
console.log("FORMULAS_A7_AM11");
console.log(JSON.stringify(sheet.getRange("A7:AM11").formulas));

const preview = await workbook.render({ sheetName: "Grille_Saisie", autoCrop: "all", scale: 1.5, format: "png" });
await fs.writeFile("grille-before.png", new Uint8Array(await preview.arrayBuffer()));
