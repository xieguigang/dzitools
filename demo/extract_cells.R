require(Erica);
require(filter);

imports "singleCell" from "Erica";
imports "machineVision" from "signalKit";

let bitmap_file = ?"--bitmap" || stop("no bitmap image input!");
let workdir = dirname(bitmap_file);

let snapshot = readImage(bitmap_file) |> filter::RTCP_gray();
let bin = machineVision::ostu(snapshot, flip = FALSE,
                            factor =0.8);

print(snapshot);
bitmap(snapshot, file = file.path(workdir, basename(bitmap_file), "cells_grayscale.bmp"));
bitmap(bin, file = file.path(workdir, basename(bitmap_file), "cells_bin.bmp"));

let cells = bin |> singleCell::HE_cells(is.binarized = TRUE,
                            flip = FALSE,
                            ostu.factor = 0.7,
                            offset = NULL,
                            noise = 0.25,
                            moran.knn = 32);

print(as.data.frame(cells));

write.csv(as.data.frame(cells), file = file.path(workdir, basename(bitmap_file), "cells_bin.csv"));

bitmap(file = file.path(workdir, basename(bitmap_file), "cells_plot.png"), size = [6400,2700]) {
    plot(cells, scatter = TRUE);
}