import fs from "node:fs/promises";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const inputPath = "D:/Downloads/Chrome/canevas-notes-2026-2027-2026-2027-bts-n1-bat-s1-classe-kribi (4).xlsx";
const outputPath = "canevas-notes-2026-2027-2026-2027-bts-n1-bat-s1-classe-kribi-notes-aleatoires.xlsx";

const input = await FileBlob.load(inputPath);
const workbook = await SpreadsheetFile.importXlsx(input);
const sheet = workbook.worksheets.getItem("Grille_Saisie");

// Notes de test déterministes, par pas de 0,25, pour obtenir un fichier reproductible.
let seed = 20260921;
const nextRandom = () => {
  seed = (seed * 1664525 + 1013904223) >>> 0;
  return seed / 4294967296;
};

const note = () => Math.round((6 + nextRandom() * 13.75) * 4) / 4;
const noteColumns = ["D:F", "H:J", "L:N", "P:R", "T:V", "X:Z", "AB:AD", "AF:AH", "AJ:AL"];

for (const row of [10, 11]) {
  for (const columns of noteColumns) {
    const [start, end] = columns.split(":");
    sheet.getRange(`${start}${row}:${end}${row}`).values = [[note(), note(), note()]];
  }
}

const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(outputPath);

const check = await workbook.inspect({
  kind: "region",
  sheetId: "Grille_Saisie",
  range: "A7:AM11",
  maxChars: 12000,
});
console.log(check.ndjson);

const errors = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!",
  options: { useRegex: true, maxResults: 100 },
  summary: "final formula error scan",
});
console.log(errors.ndjson);

const preview = await workbook.render({ sheetName: "Grille_Saisie", autoCrop: "all", scale: 1.5, format: "png" });
await fs.writeFile("grille-after.png", new Uint8Array(await preview.arrayBuffer()));
console.log(`SAVED ${outputPath}`);
