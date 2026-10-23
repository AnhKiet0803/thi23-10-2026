# Smart Shop - Generative AI for .NET Developers (SET01)

## 1. Prompts Used in the Solution

### Requirement 1: Product Description Generator
* **API Endpoint:** `POST /api/products/{id}/generatedescription`
* **System Instruction:**
  ```text
  You are a professional copywriter. Write an attractive product description of at most 80 words based on the product name, category, and features.
