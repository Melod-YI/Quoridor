package view;

import java.util.Observable;
import java.util.Observer;

import javax.swing.JButton;
import javax.swing.JFrame;
import javax.swing.JLabel;
import javax.swing.JPanel;
import javax.swing.JPasswordField;
import javax.swing.JTextField;

public class LoginFrame implements Observer{
	private JFrame loginFrame;
	private JPanel mainPanel;
	private JTextField fieldaccount;
	private JPasswordField fieldpassword;
	private JButton btnRegister;
	private JButton btnLogIn;
	private JButton btnCancel;
	private JLabel lblAccount;
	private JLabel lblpassword;
	/**
	 * Create the frame.
	 */
	public LoginFrame() {
		this.initialize();
	}
	
	public void initialize(){
		
	}
	@Override
	public void update(Observable arg0, Object arg1) {
		// TODO Auto-generated method stub
		
	}

}
