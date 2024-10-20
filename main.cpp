#include <QApplication>
#include <QWidget>
#include <QPushButton>
#include <QLineEdit>
#include <QListWidget>
#include <QMessageBox>
#include <QVBoxLayout>
#include <QLabel>
#include <vector>
#include <string>

// Estruturas para Produto e Compra
struct Produto {
    std::string nome;
    float preco;
    int quantidade; 
};

struct Compra {
    std::vector<Produto> produtos; 
    float totalCompra;
};

// Classe para a aplicação de supermercado
class SupermarketApp : public QWidget {
private:
    std::vector<Compra> historico;
    QListWidget *lista_produtos;
    QLineEdit *edit_nome;
    QLineEdit *edit_preco;
    QLineEdit *edit_quantidade;

public:
    SupermarketApp() {
        setWindowTitle("Caixa de Supermercado");
        setFixedSize(400, 300);
        
        QVBoxLayout *layout = new QVBoxLayout(this);
        
        // Campos de entrada
        edit_nome = new QLineEdit(this);
        edit_nome->setPlaceholderText("Nome do produto");
        layout->addWidget(edit_nome);

        edit_preco = new QLineEdit(this);
        edit_preco->setPlaceholderText("Preço do produto");
        layout->addWidget(edit_preco);

        edit_quantidade = new QLineEdit(this);
        edit_quantidade->setPlaceholderText("Quantidade");
        layout->addWidget(edit_quantidade);

        // Botão para adicionar produto
        QPushButton *button_adicionar = new QPushButton("Adicionar Produto", this);
        layout->addWidget(button_adicionar);

        // Lista de produtos
        lista_produtos = new QListWidget(this);
        layout->addWidget(lista_produtos);

        // Botão para finalizar compra
        QPushButton *button_finalizar = new QPushButton("Finalizar Compra", this);
        layout->addWidget(button_finalizar);

        // Conectar sinais e slots
        connect(button_adicionar, &QPushButton::clicked, this, &SupermarketApp::adicionarProduto);
        connect(button_finalizar, &QPushButton::clicked, this, &SupermarketApp::finalizarCompra);

        setLayout(layout);
    }

    void adicionarProduto() {
        QString nome = edit_nome->text();
        QString precoStr = edit_preco->text();
        QString quantidadeStr = edit_quantidade->text();

        if (nome.isEmpty() || precoStr.isEmpty() || quantidadeStr.isEmpty()) {
            QMessageBox::warning(this, "Atenção", "Preencha todos os campos.");
            return;
        }

        bool okPreco, okQuantidade;
        float preco = precoStr.toFloat(&okPreco);
        int quantidade = quantidadeStr.toInt(&okQuantidade);

        if (!okPreco || !okQuantidade || quantidade <= 0) {
            QMessageBox::warning(this, "Atenção", "Preço ou quantidade inválidos.");
            return;
        }

        Produto novoProduto = { nome.toStdString(), preco, quantidade };
        historico.back().produtos.push_back(novoProduto); // Adiciona o produto à compra atual
        lista_produtos->addItem(nome + " - R$ " + precoStr + " x " + quantidadeStr);

        edit_nome->clear();
        edit_preco->clear();
        edit_quantidade->clear();
    }

    void finalizarCompra() {
        if (historico.empty() || historico.back().produtos.empty()) {
            QMessageBox::warning(this, "Atenção", "Nenhum produto registrado.");
            return;
        }

        Compra compraAtual = historico.back();
        float total = 0.0;

        for (const auto& produto : compraAtual.produtos) {
            total += produto.preco * produto.quantidade;
        }

        QMessageBox::information(this, "Compra Finalizada", "Total da compra: R$ " + QString::number(total, 'f', 2));
        historico.push_back(Compra()); // Adiciona uma nova compra ao histórico
        lista_produtos->clear(); // Limpa a lista de produtos
    }
};

int main(int argc, char *argv[]) {
    QApplication app(argc, argv);

    SupermarketApp supermercado;
    supermercado.show();

    return app.exec();
}