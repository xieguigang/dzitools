require(Erica);
require(filter);

imports "singleCell" from "Erica";
imports "machineVision" from "signalKit";

let bitmap_file = ?"--bitmap" || stop("no bitmap image input!");
let workdir = dirname(bitmap_file);

let snapshot = readImage(bitmap_file) |> filter::adjust_contrast(-1);
let bin = machineVision::ostu(snapshot, flip = FALSE,
                            factor =0.8);

print(snapshot);
bitmap(bin, file = file.path(workdir, `cells_bin_${basename(bitmap_file)}.bmp`));

let cells = bin |> singleCell::HE_cells(is.binarized = TRUE,
                            flip = FALSE,
                            ostu.factor = 0.7,
                            offset = NULL,
                            noise = 0.25,
                            moran.knn = 32);

print(as.data.frame(cells));

write.csv(as.data.frame(cells), file = file.path(workdir, `cells_bin_${basename(bitmap_file)}.csv`));

bitmap(file = file.path(workdir, `cells_plot_${basename(bitmap_file)}.png`), size = [6400,2700]) {
    plot(cells, scatter = TRUE);
}