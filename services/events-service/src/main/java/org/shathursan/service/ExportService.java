package org.shathursan.service;

import org.springframework.stereotype.Service;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.PrintWriter;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.List;
import java.util.Map;

@Service
public class ExportService {

    /**
     * Export data to CSV format
     */
    public byte[] exportToCSV(List<Map<String, Object>> data, String[] headers) throws IOException {
        ByteArrayOutputStream outputStream = new ByteArrayOutputStream();
        PrintWriter writer = new PrintWriter(outputStream);

        // Write headers
        writer.println(String.join(",", headers));

        // Write data rows
        for (Map<String, Object> row : data) {
            StringBuilder csvRow = new StringBuilder();
            for (int i = 0; i < headers.length; i++) {
                if (i > 0) csvRow.append(",");
                Object value = row.get(headers[i]);
                String csvValue = value != null ? escapeCsvValue(value.toString()) : "";
                csvRow.append(csvValue);
            }
            writer.println(csvRow.toString());
        }

        writer.flush();
        writer.close();
        return outputStream.toByteArray();
    }

    /**
     * Export data to Excel-like format (simplified CSV with .xlsx extension)
     */
    public byte[] exportToExcel(List<Map<String, Object>> data, String[] headers) throws IOException {
        // For simplicity, return CSV data that can be opened in Excel
        // In a real implementation, you would use Apache POI library
        return exportToCSV(data, headers);
    }

    /**
     * Export data to PDF format (placeholder implementation)
     */
    public byte[] exportToPDF(List<Map<String, Object>> data, String[] headers) throws IOException {
        // Placeholder implementation
        // In a real implementation, you would use iText or similar library
        StringBuilder pdfContent = new StringBuilder();
        pdfContent.append("PDF Report - ").append(LocalDateTime.now().format(DateTimeFormatter.ISO_LOCAL_DATE_TIME)).append("\n");
        pdfContent.append("="+"=".repeat(50)).append("\n\n");
        
        // Add headers
        pdfContent.append(String.join(" | ", headers)).append("\n");
        pdfContent.append("-".repeat(80)).append("\n");
        
        // Add data
        for (Map<String, Object> row : data) {
            StringBuilder pdfRow = new StringBuilder();
            for (int i = 0; i < headers.length; i++) {
                if (i > 0) pdfRow.append(" | ");
                Object value = row.get(headers[i]);
                pdfRow.append(value != null ? value.toString() : "");
            }
            pdfContent.append(pdfRow.toString()).append("\n");
        }
        
        return pdfContent.toString().getBytes();
    }

    /**
     * Get the appropriate MIME type for the export format
     */
    public String getMimeType(String format) {
        switch (format.toLowerCase()) {
            case "csv":
                return "text/csv";
            case "excel":
            case "xlsx":
                return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            case "pdf":
                return "application/pdf";
            default:
                return "application/octet-stream";
        }
    }

    /**
     * Get the appropriate file extension for the export format
     */
    public String getFileExtension(String format) {
        switch (format.toLowerCase()) {
            case "csv":
                return ".csv";
            case "excel":
            case "xlsx":
                return ".xlsx";
            case "pdf":
                return ".pdf";
            default:
                return ".dat";
        }
    }

    private String escapeCsvValue(String value) {
        if (value == null) return "";
        
        // If value contains comma, newline, or quote, wrap in quotes and escape quotes
        if (value.contains(",") || value.contains("\n") || value.contains("\"")) {
            return "\"" + value.replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}