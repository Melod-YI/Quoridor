package view;

import java.awt.Color;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.util.Observable;
import java.util.Observer;

import javax.swing.Icon;
import javax.swing.ImageIcon;
import javax.swing.JButton;
import javax.swing.JFrame;
import javax.swing.JPanel;
import javax.swing.UIManager;
import javax.swing.UnsupportedLookAndFeelException;

public class StartFrame implements Observer{
	private JButton startButton;
	private JButton exitButton;
	private JButton helpButton;
	private JButton optionsButton;
	private Icon exit_active = new ImageIcon("images/exit_active.jpg");
	private Icon exit_hover = new ImageIcon("images/exit_hover.jpg");
	private Icon exit_normal = new ImageIcon("images/exit_normal.jpg");
	private Icon help_active = new ImageIcon("images/help_active.jpg");
	private Icon help_hover = new ImageIcon("images/help_hover.jpg");
	private Icon help_normal = new ImageIcon("images/help_normal.jpg");
	private Icon options_active = new ImageIcon("images/options_active.jpg");
	private Icon options_hover = new ImageIcon("images/options_hover.jpg");
	private Icon options_normal = new ImageIcon("images/options_normal.jpg");
	private Icon start_active = new ImageIcon("images/start_active.jpg");
	private Icon start_hover = new ImageIcon("images/start_hover.jpg");
	private Icon start_normal = new ImageIcon("images/start_normal.jpg");
	private JPanel head;
	private JPanel body;
	private JFrame startFrame; 
	private void componentsInstantiation(){
		startFrame=new JFrame();
		head=new JPanel();
		body=new JPanel();
		startButton=new JButton();
		exitButton=new JButton();
		helpButton=new JButton();
		optionsButton=new JButton();
	}
	
	private void initComponents(){
		startButton.setIcon(start_normal);
		startButton.setSelectedIcon(start_hover);
		startButton.setPressedIcon(start_active);
		exitButton.setIcon(exit_normal);
		exitButton.setSelectedIcon(exit_hover);
		exitButton.setPressedIcon(exit_active);
		optionsButton.setIcon(options_normal);
		optionsButton.setSelectedIcon(options_hover);
		optionsButton.setPressedIcon(options_active);
		helpButton.setIcon(help_normal);
		helpButton.setSelectedIcon(help_hover);
		helpButton.setPressedIcon(help_active);
		
		addbuttonlistener();
		
		startFrame.setBackground(Color.WHITE);
		startFrame
		.setDefaultCloseOperation(javax.swing.WindowConstants.EXIT_ON_CLOSE);
		startFrame.setResizable(false);
		
		
		
		
	}
	@Override
	public void update(Observable arg0, Object arg1) {
		// TODO Auto-generated method stub
		
	}
	
	
	private void addbuttonlistener(){
		startButton.addActionListener(new ActionListener(){
			@Override
			public void actionPerformed(ActionEvent arg0) {
				// TODO Auto-generated method stub
				
			}
		});
		exitButton.addActionListener(new ActionListener(){
			@Override
			public void actionPerformed(ActionEvent arg0) {
				// TODO Auto-generated method stub
				
			}
		});
		optionsButton.addActionListener(new ActionListener(){
			@Override
			public void actionPerformed(ActionEvent arg0) {
				// TODO Auto-generated method stub
				
			}
		});
		helpButton.addActionListener(new ActionListener(){
			@Override
			public void actionPerformed(ActionEvent arg0) {
				// TODO Auto-generated method stub
				
			}
		});
		
		
	}
	
	
	
	public StartFrame() throws ClassNotFoundException, InstantiationException, IllegalAccessException, UnsupportedLookAndFeelException{
		UIManager.setLookAndFeel(UIManager.getSystemLookAndFeelClassName());
		componentsInstantiation();
		initComponents();
		startFrame.setVisible(true);
	}
	
}
